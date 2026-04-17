using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using Photon.Realtime;
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
    private readonly Dictionary<Vector3Int, BuildingSaveData> _pendingClientBuildingSnapshots =
        new Dictionary<Vector3Int, BuildingSaveData>();
    private BuildingPlacementService _placement;
    private IBuildingInstanceFactory _factory;
    private int _completedBuildingCount;

    public int BuildingCount => _completedBuildingCount;
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
        _factory = CreateFactory();
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
            RunLocalBuildAsync(request).Forget();
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            RunLocalBuildAsync(request).Forget();
            return;
        }

        photonView.RpcSafe(nameof(RPC_RequestBuild), RpcTarget.MasterClient,
                           request.Data.BuildingId,
                           request.AnchorPos.x, request.AnchorPos.y, request.AnchorPos.z,
                           request.Direction);
    }

    public void RequestRemove(Vector3Int anyPos)
    {
        if (!PhotonNetwork.IsConnected)
        {
            RunLocalRemove(anyPos);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            RunLocalRemove(anyPos);
            return;
        }

        photonView.RpcSafe(nameof(RPC_RequestRemove), RpcTarget.MasterClient,
                           anyPos.x, anyPos.y, anyPos.z);
    }

    // offline/master 로컬 빌드 — RPC 왕복 없이 accepted 의미의 비용 이벤트만 로컬에서 발생.
    private async UniTaskVoid RunLocalBuildAsync(BuildingRequest request)
    {
        if (await TryBuildAuthoritative(request))
        {
            OnLocalBuildCostConfirmed?.Invoke(request.Data);
        }
    }

    private void RunLocalRemove(Vector3Int anyPos)
    {
        if (!TryResolveBuildingInfoFromPosition(anyPos, out BuildingInfo info)) return;
        if (!TryRemoveAuthoritative(info)) return;
        OnLocalRemoveRefundGranted?.Invoke(info.Data);
    }

    [PunRPC]
    private void RPC_RequestBuild(string buildingId, int ax, int ay, int az, int direction, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        BuildingDataSO data = _buildingDatabase.GetById(buildingId);
        if (data == null)
        {
            RespondBuildRejected(info.Sender, "unknown-building-id");
            return;
        }

        var request = new BuildingRequest
        {
            Data = data,
            AnchorPos = new Vector3Int(ax, ay, az),
            Direction = direction
        };

        RunAuthoritativeBuildForSender(request, info.Sender).Forget();
    }

    // accepted/rejected는 실제 spawn 성공 여부를 기다린 뒤 보낸다.
    private async UniTaskVoid RunAuthoritativeBuildForSender(BuildingRequest request, Player sender)
    {
        bool success = await TryBuildAuthoritative(request);
        if (sender == null) return;

        if (success)
            photonView.RPC(nameof(RPC_BuildAccepted), sender, request.Data.BuildingId);
        else
            photonView.RPC(nameof(RPC_BuildRejected), sender, "build-rejected");
    }

    [PunRPC]
    private void RPC_RequestRemove(int x, int y, int z, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var pos = new Vector3Int(x, y, z);
        if (!TryResolveBuildingInfoFromPosition(pos, out BuildingInfo bi))
        {
            RespondRemoveRejected(info.Sender, "target-not-found");
            return;
        }

        bool success = TryRemoveAuthoritative(bi);
        if (info.Sender == null) return;

        if (success)
            photonView.RPC(nameof(RPC_RemoveAccepted), info.Sender, bi.Data.BuildingId);
        else
            photonView.RPC(nameof(RPC_RemoveRejected), info.Sender, "remove-rejected");
    }

    private void RespondBuildRejected(Player target, string reason)
    {
        if (target == null) return;
        photonView.RPC(nameof(RPC_BuildRejected), target, reason);
    }

    private void RespondRemoveRejected(Player target, string reason)
    {
        if (target == null) return;
        photonView.RPC(nameof(RPC_RemoveRejected), target, reason);
    }

    [PunRPC]
    private void RPC_BuildAccepted(string buildingId)
    {
        BuildingDataSO data = _buildingDatabase.GetById(buildingId);
        if (data == null) return;
        OnLocalBuildCostConfirmed?.Invoke(data);
    }

    [PunRPC]
    private void RPC_BuildRejected(string reason)
    {
        LogBuildingSync($"Build rejected reason={reason}");
    }

    [PunRPC]
    private void RPC_RemoveAccepted(string buildingId)
    {
        BuildingDataSO data = _buildingDatabase.GetById(buildingId);
        if (data == null) return;
        OnLocalRemoveRefundGranted?.Invoke(data);
    }

    [PunRPC]
    private void RPC_RemoveRejected(string reason)
    {
        LogBuildingSync($"Remove rejected reason={reason}");
    }
    #endregion

    #region BuildingInfo Helpers
    // 신규 건설 요청을 검증 후 BuildingInfo로 승격한다. placement 실패 시 false.
    private bool TryCreateNewBuildingInfo(BuildingRequest request, out BuildingInfo info)
    {
        info = default;
        if (request.Data == null) return false;

        BuildingFootprint footprint = BuildingPlacer.GetFootprint(request.Data, request.Direction);
        if (!_placement.CanPlace(request.AnchorPos, footprint, out int baseY)) return false;

        var anchor = new Vector3Int(request.AnchorPos.x, baseY, request.AnchorPos.z);
        var saveData = new BuildingSaveData
        {
            BuildingId = request.Data.BuildingId,
            AnchorX = anchor.x,
            AnchorY = baseY,
            AnchorZ = anchor.z,
            Direction = request.Direction,
            RemainingDays = request.Data.ConstructionDays
        };

        info = new BuildingInfo(request.Data, saveData, anchor, footprint);
        return true;
    }

    // 기존 saveData를 도메인 해석한다. placement 검증은 하지 않는다(복원/snapshot 용도).
    private bool TryResolveBuildingInfo(BuildingSaveData saveData, out BuildingInfo info)
    {
        info = default;
        if (saveData == null) return false;

        BuildingDataSO data = _buildingDatabase.GetById(saveData.BuildingId);
        if (data == null) return false;

        BuildingFootprint footprint = BuildingPlacer.GetFootprint(data, saveData.Direction);
        var anchor = new Vector3Int(saveData.AnchorX, saveData.AnchorY, saveData.AnchorZ);
        info = new BuildingInfo(data, saveData, anchor, footprint);
        return true;
    }

    private bool TryResolveBuildingInfoFromPosition(Vector3Int anyPos, out BuildingInfo info)
    {
        info = default;
        if (!_registry.TryGetAnchor(anyPos, out Vector3Int anchor)) return false;
        if (!_registry.TryGetSaveData(anchor, out BuildingSaveData saveData)) return false;
        return TryResolveBuildingInfo(saveData, out info);
    }

    // PhotonView.InstantiationData를 BuildingSaveData로 복원한다.
    private static bool TryCreateBuildingSaveDataFromInstantiationData(object[] data, out BuildingSaveData saveData)
    {
        saveData = null;
        if (data == null || data.Length < 6) return false;

        string buildingId = data[0] as string;
        if (string.IsNullOrEmpty(buildingId)) return false;

        saveData = new BuildingSaveData
        {
            BuildingId = buildingId,
            AnchorX = Convert.ToInt32(data[1]),
            AnchorY = Convert.ToInt32(data[2]),
            AnchorZ = Convert.ToInt32(data[3]),
            Direction = Convert.ToInt32(data[4]),
            RemainingDays = Convert.ToInt32(data[5]),
        };
        return true;
    }
    #endregion

    #region State Registration
    // save/placement 등록만 담당. instance bind 전의 예약 상태.
    private void RegisterBuildingState(in BuildingInfo info)
    {
        _registry.RegisterBuilding(info.Anchor, info.SaveData);
        _placement.MarkOccupied(info.Anchor, info.Footprint);
    }

    // instance 바인딩 + 이벤트 구독 + storage 등록 + Initialize 호출.
    private void BindBuildingInstance(in BuildingInfo info, BaseBuilding instance, bool onLoading)
    {
        _registry.RegisterInstance(info.Anchor, instance);
        instance.ConstructionCompleted -= HandleBuildingConstructionCompleted;
        instance.ConstructionCompleted += HandleBuildingConstructionCompleted;
        instance.Initialize(info.Data, info.SaveData, CreateConstructionContext(), onLoading);
        RegisterStorageIfPresent(info.SaveData, instance);
    }

    // registry/placement/instance/pending snapshot을 모두 정리한다. spawn 롤백과 삭제에서 공용.
    private void UnregisterBuildingState(in BuildingInfo info)
    {
        _registry.RemoveInstance(info.Anchor, out _);
        _registry.RemoveBuilding(info.Anchor);
        _placement.ClearOccupied(info.Anchor, info.Footprint);
        _pendingClientBuildingSnapshots.Remove(info.Anchor);
    }
    #endregion

    #region Authoritative Build / Remove
    /// 신규 건설 authoritative 경로. Master 또는 offline에서만 실제 spawn을 수행한다.
    /// factory 실패 시 registry/placement가 롤백되어 상태 불일치가 남지 않는다.
    private async UniTask<bool> TryBuildAuthoritative(BuildingRequest request)
    {
        if (!TryCreateNewBuildingInfo(request, out BuildingInfo info)) return false;

        bool ok = await SpawnAndInitializeBuilding(info, onLoading: false);
        if (ok)
        {
            LogBuildingSync(
                $"TryBuild id={info.SaveData.BuildingId} anchor={info.Anchor} remaining={info.SaveData.RemainingDays}/{info.Data.ConstructionDays}");
        }
        return ok;
    }

    /// Master 또는 offline에서 실제 파괴를 수행한다.
    /// factory.Destroy가 true를 반환했을 때만 registry/placement를 해제한다.
    private bool TryRemoveAuthoritative(in BuildingInfo info)
    {
        if (!_registry.TryGetInstance(info.Anchor, out BaseBuilding instance) || instance == null)
            return false;

        // 완료 전 파괴된 건물은 애초에 count에 잡히지 않았으므로 감소시키지 않는다.
        bool wasCompleted = instance.IsConstructionComplete;
        if (!_factory.Destroy(instance)) return false;

        UnregisterBuildingState(info);
        if (wasCompleted) _completedBuildingCount--;
        OnBuildingDestroyed?.Invoke();
        LogBuildingSync($"TryRemove id={info.SaveData.BuildingId} anchor={info.Anchor} completed={wasCompleted}");
        return true;
    }

    // registry/placement를 먼저 예약한 뒤 factory 생성. factory 예외는 bool로 흡수해
    // caller의 RPC 응답이 항상 보내지도록 한다(상위 UniTaskVoid에서 유실 방지).
    private async UniTask<bool> SpawnAndInitializeBuilding(BuildingInfo info, bool onLoading)
    {
        RegisterBuildingState(info);

        BaseBuilding instance;
        try
        {
            float yRot = info.Footprint.Direction * 90f;
            Vector3 spawnPos = _placement.CalculateSpawnPos(info.Anchor, info.Footprint);
            instance = await _factory.CreateAsync(info, spawnPos, Quaternion.Euler(0f, yRot, 0f));
        }
        catch (Exception ex)
        {
            UnregisterBuildingState(info);
            Debug.LogException(ex);
            LogBuildingSync($"SpawnAndInitializeBuilding failed id={info.SaveData?.BuildingId} anchor={info.Anchor}");
            return false;
        }

        if (instance == null)
        {
            UnregisterBuildingState(info);
            return false;
        }

        BindBuildingInstance(info, instance, onLoading);
        return true;
    }

    private void HandleBuildingConstructionCompleted(BaseBuilding building)
    {
        if (building == null) return;
        building.ConstructionCompleted -= HandleBuildingConstructionCompleted;
        _completedBuildingCount++;
        OnBuildingBuilt?.Invoke(building.BuildingData);
    }
    #endregion

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
                instance.InitializeNpcAnchorsIfNeeded();
                instance.SetNpc(spawner.SpawnNpc(kvp.Value));
            }
        }
    }

    public List<BuildingSaveData> ExportBuildings()
    {
        List<BuildingSaveData> exported = _registry.ExportAll();
        LogBuildingSync($"Export count={exported.Count}");
        return exported;
    }

    public async UniTask ImportBuildings(List<BuildingSaveData> list)
    {
        if (list == null) return;

        // 네트워크 클라이언트는 Photon instantiate 자동 복제로 인스턴스를 받고,
        // ImportBuildings는 snapshot을 기존 인스턴스에 적용하거나 pending으로 저장한다.
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
        {
            ApplyClientSnapshots(list);
            return;
        }

        LogBuildingSync($"ImportBuildings(master/offline) count={list.Count}");
        foreach (BuildingSaveData saveData in list)
        {
            if (!TryResolveBuildingInfo(saveData, out BuildingInfo info)) continue;
            await SpawnAndInitializeBuilding(info, onLoading: true);
        }
    }

    private void ApplyClientSnapshots(List<BuildingSaveData> list)
    {
        var snapshotAnchors = new HashSet<Vector3Int>(list.Count);

        foreach (BuildingSaveData saveData in list)
        {
            if (saveData == null) continue;
            var anchor = new Vector3Int(saveData.AnchorX, saveData.AnchorY, saveData.AnchorZ);
            snapshotAnchors.Add(anchor);

            if (_registry.TryGetInstance(anchor, out BaseBuilding instance) && instance != null)
            {
                if (_registry.TryGetSaveData(anchor, out BuildingSaveData existing) && existing != null)
                {
                    existing.RemainingDays = saveData.RemainingDays;
                }
                instance.ApplyConstructionRemainingDays(saveData.RemainingDays);
                LogBuildingSync($"Client snapshot applied anchor={anchor} remaining={saveData.RemainingDays}");
            }
            else
            {
                _pendingClientBuildingSnapshots[anchor] = saveData;
                LogBuildingSync($"Client snapshot pending anchor={anchor} remaining={saveData.RemainingDays}");
            }
        }

        // 맵 동기화 시 더 이상 존재하지 않는 건물의 stale pending snapshot을 정리한다.
        if (_pendingClientBuildingSnapshots.Count > 0)
        {
            List<Vector3Int> stale = null;
            foreach (Vector3Int anchor in _pendingClientBuildingSnapshots.Keys)
            {
                if (snapshotAnchors.Contains(anchor)) continue;
                stale ??= new List<Vector3Int>();
                stale.Add(anchor);
            }
            if (stale != null)
            {
                foreach (Vector3Int anchor in stale)
                {
                    _pendingClientBuildingSnapshots.Remove(anchor);
                }
                LogBuildingSync($"Client snapshot stale cleared count={stale.Count}");
            }
        }
    }
    #endregion

    /// <summary>
    /// 마스터의 PhotonNetwork.Instantiate로 클라이언트에 자동 생성된 건물을 _registry에 등록한다.
    /// PhotonView.InstantiationData에서 BuildingSaveData를 복원해 공통 bind helper로 초기화한다.
    /// 마스터는 TryBuildAuthoritative 경로에서 처리하므로 이 메서드는 클라이언트 전용.
    /// </summary>
    public void RegisterClientSpawnedBuilding(BaseBuilding instance)
    {
        if (instance == null) return;
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient) return;

        PhotonView pv = instance.GetComponent<PhotonView>();
        if (pv == null) return;

        if (!TryCreateBuildingSaveDataFromInstantiationData(pv.InstantiationData, out BuildingSaveData saveData)) return;
        if (!TryResolveBuildingInfo(saveData, out BuildingInfo info)) return;

        // 맵 동기화 snapshot이 Photon instantiate보다 먼저 도착한 경우 pending snapshot의 RemainingDays로 덮어쓴다.
        if (_pendingClientBuildingSnapshots.TryGetValue(info.Anchor, out BuildingSaveData pending) && pending != null)
        {
            info.SaveData.RemainingDays = pending.RemainingDays;
            _pendingClientBuildingSnapshots.Remove(info.Anchor);
            LogBuildingSync($"Client pending snapshot consumed anchor={info.Anchor} remaining={info.SaveData.RemainingDays}");
        }

        RegisterBuildingState(info);
        BindBuildingInstance(info, instance, onLoading: true);
        LogBuildingSync($"RegisterClientSpawnedBuilding anchor={info.Anchor} remaining={info.SaveData.RemainingDays}");
    }

    /// <summary>
    /// 클라이언트에서 PhotonNetwork.Destroy 자동 복제로 파괴된 건물의 _registry/_placement 정리.
    /// 마스터는 TryRemoveAuthoritative 경로에서 처리하므로 이 메서드는 클라이언트 전용.
    /// BaseBuilding.OnDestroy에서 호출.
    /// </summary>
    public void UnregisterClientSpawnedBuilding(BaseBuilding instance)
    {
        if (instance == null || instance.SaveData == null) return;
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient) return;

        if (!TryResolveBuildingInfo(instance.SaveData, out BuildingInfo info)) return;

        bool hadInstance = _registry.TryGetInstance(info.Anchor, out _);
        bool wasCompleted = instance.IsConstructionComplete;
        UnregisterBuildingState(info);
        if (hadInstance)
        {
            if (wasCompleted) _completedBuildingCount--;
            OnBuildingDestroyed?.Invoke();
        }
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

    private static IBuildingInstanceFactory CreateFactory()
    {
        // 네트워크 연결 상태에서 PhotonNetwork.Instantiate를 사용한다. 싱글/오프라인은 Local factory.
        // 런타임 중 연결 상태가 바뀌는 경우는 전제하지 않는다 (씬 전환 단위).
        return PhotonNetwork.IsConnected
            ? new PhotonBuildingInstanceFactory()
            : new LocalBuildingInstanceFactory();
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogBuildingSync(string message)
    {
        Debug.Log($"[BuildingSync] {message}");
    }
}
