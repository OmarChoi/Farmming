using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class BuildingManager : MonoBehaviourPunCallbacks
{
    private const string RevealLitShaderName = "Custom/Construction/RevealLit";
    private const string GhostRevealShaderName = "Custom/Construction/GhostReveal";

    public static BuildingManager Instance { get; private set; }

    [SerializeField] private TerrainGridManager _gridManager;
    [SerializeField] private BuildingDatabase _buildingDatabase;
    [Header("Construction Visuals")]
    [SerializeField] private Shader _constructionRevealLitShader;
    [SerializeField] private Shader _constructionGhostRevealShader;
    [SerializeField] private Material _ghostMaterial;
    [SerializeField] private Color _ghostValidColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color _ghostInvalidColor = new Color(1f, 0f, 0f, 0.5f);

    public event Action<BuildingDataSO> OnLocalBuildCostConfirmed;
    public event Action<BuildingDataSO> OnLocalRemoveRefundGranted;
    public event Action<BuildingDataSO> OnBuildingBuilt;
    public event Action OnBuildingDestroyed;

    private readonly BuildingRegistry _registry = new BuildingRegistry();
    private BuildingPlacementService _placement;
    private BuildingInstanceFactory _factory;

    public int BuildingCount { get; private set; }
    public IReadOnlyList<BuildingDataSO> AvailableBuildings => _buildingDatabase.Buildings;
    public GhostConfig GhostConfig => new GhostConfig(_ghostMaterial, _ghostValidColor, _ghostInvalidColor);

    private BuildingConstructionContext CreateConstructionContext()
    {
        return new BuildingConstructionContext(
            _constructionRevealLitShader,
            _constructionGhostRevealShader,
            _ghostMaterial);
    }

    #region Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _placement = new BuildingPlacementService(_gridManager, _registry);
        _factory = new BuildingInstanceFactory();
        ResolveConstructionVisualReferences();
        ValidateConstructionVisualReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Reset()
    {
        ResolveConstructionVisualReferences();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        ResolveConstructionVisualReferences();
#endif
    }
    #endregion

    #region Query
    public bool IsOccupied(Vector3Int gridPos) => _registry.IsOccupied(gridPos);

    public bool IsConstructionComplete(Vector3Int anyPos) => _registry.IsConstructionComplete(anyPos);
    #endregion

    #region Ghost Preview
    public BuildingPreviewInfo GetPreviewInfo(Vector3Int anchorPos, BuildingDataSO data, int direction)
    {
        BuildingFootprint footprint = BuildingPlacer.GetFootprint(data, direction);
        bool canPlace = _placement.CanPlace(anchorPos, footprint, out int baseY);

        var adjustedAnchor = new Vector3Int(anchorPos.x, baseY >= 0 ? baseY : anchorPos.y, anchorPos.z);
        Vector3 spawnPos = _placement.CalculateSpawnPos(adjustedAnchor, footprint);
        float yRot = footprint.Direction * 90f;

        return new BuildingPreviewInfo
        {
            SpawnPosition = spawnPos,
            Rotation = Quaternion.Euler(0f, yRot, 0f),
            CanPlace = canPlace
        };
    }
    #endregion

    #region Network Request
    public void RequestBuild(BuildingRequest request)
    {
        if (!PhotonNetwork.IsConnected)
        {
            TryBuildAndConfirmLocal(request).Forget();
            return;
        }

        photonView.RpcSafe(nameof(RPC_RequestBuild), RpcTarget.MasterClient,
                           request.Data.BuildingId,
                           request.AnchorPos.x, request.AnchorPos.y, request.AnchorPos.z,
                           request.Direction);
    }

    private async UniTaskVoid TryBuildAndConfirmLocal(BuildingRequest request)
    {
        if (await TryBuild(request))
        {
            OnLocalBuildCostConfirmed?.Invoke(request.Data);
        }
    }

    public void RequestRemove(Vector3Int anyPos)
    {
        if (!PhotonNetwork.IsConnected)
        {
            if (TryRemove(anyPos, out BuildingDataSO buildingData))
            {
                NotifyLocalRemoveRefund(buildingData);
            }
            return;
        }

        photonView.RpcSafe(nameof(RPC_RequestRemove), RpcTarget.MasterClient,
                           anyPos.x, anyPos.y, anyPos.z);
    }

    [PunRPC]
    private void RPC_RequestBuild(string buildingId, int ax, int ay, int az, int direction, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        BuildingDataSO data = _buildingDatabase.GetById(buildingId);
        if (data == null) return;

        var anchorPos = new Vector3Int(ax, ay, az);
        BuildingFootprint footprint = BuildingPlacer.GetFootprint(data, direction);
        if (!_placement.CanPlace(anchorPos, footprint, out _)) return;

        if (info.Sender != null)
        {
            photonView.RPC(nameof(RPC_ConfirmBuild), info.Sender, buildingId);
        }

        // 마스터에서 직접 TryBuild → PhotonNetwork.Instantiate 자동 복제로 클라이언트에 전파.
        // 클라이언트는 BaseBuilding.Start에서 RegisterClientSpawnedBuilding으로 자가 등록.
        var request = new BuildingRequest
        {
            Data = data,
            AnchorPos = anchorPos,
            Direction = direction
        };
        TryBuild(request).Forget();
    }

    [PunRPC]
    private void RPC_ConfirmBuild(string buildingId)
    {
        BuildingDataSO data = _buildingDatabase.GetById(buildingId);
        if (data == null) return;

        OnLocalBuildCostConfirmed?.Invoke(data);
    }

    [PunRPC]
    private void RPC_RequestRemove(int x, int y, int z, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var pos = new Vector3Int(x, y, z);
        if (!TryGetBuildingInfo(pos, out Vector3Int anchor, out BuildingSaveData saveData, out BuildingDataSO buildingData)) return;

        // 마스터에서 직접 파괴 → PhotonNetwork.Destroy 자동 복제로 클라이언트에 전파.
        // 클라이언트는 BaseBuilding.OnDestroy에서 UnregisterClientSpawnedBuilding으로 _registry 정리.
        if (!TryRemoveResolved(anchor, saveData, buildingData)) return;

        if (info.Sender != null)
        {
            photonView.RPC(nameof(RPC_RefundRemove), info.Sender, buildingData.BuildingId);
        }
    }

    [PunRPC]
    private void RPC_RefundRemove(string buildingId)
    {
        BuildingDataSO buildingData = _buildingDatabase.GetById(buildingId);
        if (buildingData == null) return;

        NotifyLocalRemoveRefund(buildingData);
    }
    #endregion

    #region Build Object
    public async UniTask<bool> TryBuild(BuildingRequest request)
    {
        BuildingFootprint footprint = BuildingPlacer.GetFootprint(request.Data, request.Direction);
        if (!_placement.CanPlace(request.AnchorPos, footprint, out int baseY)) return false;

        var anchor = new Vector3Int(request.AnchorPos.x, baseY, request.AnchorPos.z);

        BuildingSaveData saveData = new BuildingSaveData
        {
            BuildingId = request.Data.BuildingId,
            AnchorX = request.AnchorPos.x,
            AnchorY = baseY,
            AnchorZ = request.AnchorPos.z,
            Direction = request.Direction,
            RemainingDays = request.Data.ConstructionDays
        };

        _registry.RegisterBuilding(anchor, saveData);
        _placement.MarkOccupied(anchor, footprint);

        float yRot = footprint.Direction * 90f;
        Vector3 spawnPos = _placement.CalculateSpawnPos(anchor, footprint);
        BaseBuilding instance = await _factory.CreateAsync
        (
            request.Data,
            spawnPos,
            Quaternion.Euler(0f, yRot, 0f),
            saveData
        );

        if (instance != null)
        {
            _registry.RegisterInstance(anchor, instance);
            instance.ConstructionCompleted -= HandleBuildingConstructionCompleted;
            instance.ConstructionCompleted += HandleBuildingConstructionCompleted;
            instance.Initialize(request.Data, saveData, CreateConstructionContext());
            RegisterStorageIfPresent(saveData, instance);
        }

        return true;
    }

    private void HandleBuildingConstructionCompleted(BaseBuilding building)
    {
        if (building == null) return;
        building.ConstructionCompleted -= HandleBuildingConstructionCompleted;
        BuildingCount++;
        OnBuildingBuilt?.Invoke(building.BuildingData);
    }

    private bool TryGetBuildingInfo(
        Vector3Int anyPos,
        out Vector3Int anchor,
        out BuildingSaveData saveData,
        out BuildingDataSO buildingData
    )
    {
        anchor = default;
        saveData = null;
        buildingData = null;

        if (!_registry.TryGetAnchor(anyPos, out anchor)) return false;
        if (!_registry.TryGetSaveData(anchor, out saveData)) return false;

        buildingData = _buildingDatabase.GetById(saveData.BuildingId);
        return buildingData != null;
    }

    public bool TryRemove(Vector3Int anyPos, out BuildingDataSO buildingData)
    {
        if (!TryGetBuildingInfo(anyPos, out Vector3Int anchor, out BuildingSaveData saveData, out buildingData))
        {
            return false;
        }

        return TryRemoveResolved(anchor, saveData, buildingData);
    }

    private void DestroyBuildingInstance(Vector3Int anchor)
    {
        if (!_registry.RemoveInstance(anchor, out BaseBuilding buildingInstance)) return;
        _factory.Destroy(buildingInstance);
        BuildingCount--;
        OnBuildingDestroyed?.Invoke();
    }

    private bool TryRemoveResolved(Vector3Int anchor, BuildingSaveData saveData, BuildingDataSO buildingData)
    {
        if (saveData == null || buildingData == null) return false;

        BuildingFootprint footprint = BuildingPlacer.GetFootprint(buildingData, saveData.Direction);

        DestroyBuildingInstance(anchor);
        _placement.ClearOccupied(anchor, footprint);
        _registry.RemoveBuilding(anchor);
        return true;
    }
    #endregion

    private void NotifyLocalRemoveRefund(BuildingDataSO buildingData)
    {
        if (buildingData == null) return;
        OnLocalRemoveRefundGranted?.Invoke(buildingData);
    }

    private void ResolveConstructionVisualReferences()
    {
        if (_constructionRevealLitShader == null)
        {
            _constructionRevealLitShader = Shader.Find(RevealLitShaderName);
        }

        if (_constructionGhostRevealShader == null)
        {
            _constructionGhostRevealShader = Shader.Find(GhostRevealShaderName);
        }
    }

    private void ValidateConstructionVisualReferences()
    {
        if (_constructionRevealLitShader == null)
        {
            Debug.LogError($"{nameof(BuildingManager)} could not resolve {RevealLitShaderName}. Assign the reveal shader in the inspector so construction visuals work in builds.", this);
        }

        if (_constructionGhostRevealShader == null)
        {
            Debug.LogError($"{nameof(BuildingManager)} could not resolve {GhostRevealShaderName}. Assign the ghost reveal shader in the inspector so construction visuals work in builds.", this);
        }

        if (_ghostMaterial == null)
        {
            Debug.LogWarning($"{nameof(BuildingManager)} is missing a ghost material. Preview and construction overlays will use the shader fallback tint.", this);
        }
    }

    #region Sync
    public void SpawnBuildingNpcs()
    {
        foreach (KeyValuePair<Vector3Int, BuildingSaveData> kvp in _registry.AllBuildings)
        {
            if (kvp.Value.RemainingDays > 0) continue;
            if (!_registry.TryGetInstance(kvp.Key, out BaseBuilding instance) || instance == null) continue;

            BuildingNpcSpawner spawner = instance.GetComponent<BuildingNpcSpawner>();
            if (spawner != null && spawner.HasValidData)
            {
                instance.SetNpc(spawner.SpawnNpc());
            }
        }
    }

    public List<BuildingSaveData> ExportBuildings() => _registry.ExportAll();

    public async UniTask ImportBuildings(List<BuildingSaveData> list)
    {
        if (list == null) return;

        // 클라이언트는 skip — 마스터의 PhotonNetwork.Instantiate 자동 복제로 인스턴스를 받는다.
        // (BaseBuilding.Start에서 InstantiationData를 읽어 RegisterClientSpawnedBuilding 호출)
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;

        foreach (BuildingSaveData saveData in list)
        {
            BuildingDataSO data = _buildingDatabase.GetById(saveData.BuildingId);
            if (data == null) continue;

            var request = new BuildingRequest
            {
                Data = data,
                AnchorPos = new Vector3Int(saveData.AnchorX, saveData.AnchorY, saveData.AnchorZ),
                Direction = saveData.Direction
            };
            await TryBuild(request);

            var anchor = new Vector3Int(saveData.AnchorX, saveData.AnchorY, saveData.AnchorZ);
            if (!_registry.TryGetSaveData(anchor, out BuildingSaveData built)) continue;

            built.RemainingDays = saveData.RemainingDays;
            if (_registry.TryGetInstance(anchor, out BaseBuilding baseBuildingInstance))
            {
                baseBuildingInstance.Initialize(data, built, CreateConstructionContext(), true);
            }
        }
    }
    #endregion

    /// <summary>
    /// 마스터의 PhotonNetwork.Instantiate로 클라이언트에 자동 생성된 건물을 _registry에 등록한다.
    /// PhotonView.InstantiationData에서 BuildingSaveData를 복원해 Initialize까지 수행.
    /// 마스터는 TryBuild 경로에서 처리하므로 이 메서드는 클라이언트 전용.
    /// </summary>
    public void RegisterClientSpawnedBuilding(BaseBuilding instance)
    {
        if (instance == null) return;
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient) return;

        PhotonView pv = instance.GetComponent<PhotonView>();
        if (pv == null) return;

        object[] data = pv.InstantiationData;
        if (data == null || data.Length < 6) return;

        string buildingId = data[0] as string;
        if (string.IsNullOrEmpty(buildingId)) return;

        BuildingDataSO buildingData = _buildingDatabase.GetById(buildingId);
        if (buildingData == null) return;

        var saveData = new BuildingSaveData
        {
            BuildingId = buildingId,
            AnchorX = (int)data[1],
            AnchorY = (int)data[2],
            AnchorZ = (int)data[3],
            Direction = (int)data[4],
            RemainingDays = (int)data[5],
        };

        var anchor = new Vector3Int(saveData.AnchorX, saveData.AnchorY, saveData.AnchorZ);
        BuildingFootprint footprint = BuildingPlacer.GetFootprint(buildingData, saveData.Direction);

        _registry.RegisterBuilding(anchor, saveData);
        _placement.MarkOccupied(anchor, footprint);
        _registry.RegisterInstance(anchor, instance);

        instance.ConstructionCompleted -= HandleBuildingConstructionCompleted;
        instance.ConstructionCompleted += HandleBuildingConstructionCompleted;
        instance.Initialize(buildingData, saveData, CreateConstructionContext(), true);

        RegisterStorageIfPresent(saveData, instance);
    }

    /// <summary>
    /// 클라이언트에서 PhotonNetwork.Destroy 자동 복제로 파괴된 건물의 _registry/_placement 정리.
    /// 마스터는 TryRemoveResolved 경로에서 처리하므로 이 메서드는 클라이언트 전용.
    /// BaseBuilding.OnDestroy에서 호출.
    /// </summary>
    public void UnregisterClientSpawnedBuilding(BaseBuilding instance)
    {
        if (instance == null || instance.SaveData == null) return;
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient) return;

        BuildingSaveData saveData = instance.SaveData;
        BuildingDataSO buildingData = _buildingDatabase.GetById(saveData.BuildingId);
        if (buildingData == null) return;

        var anchor = new Vector3Int(saveData.AnchorX, saveData.AnchorY, saveData.AnchorZ);
        BuildingFootprint footprint = BuildingPlacer.GetFootprint(buildingData, saveData.Direction);

        if (_registry.RemoveInstance(anchor, out _))
        {
            BuildingCount--;
            OnBuildingDestroyed?.Invoke();
        }
        _placement.ClearOccupied(anchor, footprint);
        _registry.RemoveBuilding(anchor);
    }

    private static void RegisterStorageIfPresent(BuildingSaveData saveData, BaseBuilding buildingInstance)
    {
        if (saveData == null || buildingInstance == null) return;

        StorageObject[] storages = buildingInstance.GetComponentsInChildren<StorageObject>(true);
        if (storages == null || storages.Length == 0) return;

        if (storages.Length > 1)
        {
            Debug.LogWarning($"{nameof(BuildingManager)} found multiple {nameof(StorageObject)} components under building {saveData.BuildingId} at ({saveData.AnchorX}, {saveData.AnchorY}, {saveData.AnchorZ}). Only the first storage will be saved.");
        }

        StorageManager.Instance.RegisterBuildingStorage(saveData, storages[0]);
    }

}
