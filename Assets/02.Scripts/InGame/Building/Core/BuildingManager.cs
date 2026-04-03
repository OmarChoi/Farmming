using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class BuildingManager : MonoBehaviourPunCallbacks
{
    public static BuildingManager Instance { get; private set; }

    [SerializeField] private TerrainGridManager _gridManager;
    [SerializeField] private BuildingDatabase _buildingDatabase;

    public BuildingDataSO SelectedBuilding { get; private set; }
    public event Action<BuildingDataSO> OnBuildingSelected;
    public event Action<BuildingDataSO> OnLocalBuildCostConfirmed;
    public event Action<BuildingDataSO> OnLocalRemoveRefundGranted;

    // anchorPos -> building save data used for removal, persistence, and restore.
    private readonly Dictionary<Vector3Int, BuildingSaveData> _buildings = new Dictionary<Vector3Int, BuildingSaveData>();
    // occupied cell -> anchor position mapping so any footprint cell can resolve the full building.
    private readonly Dictionary<Vector3Int, Vector3Int> _occupiedCells = new Dictionary<Vector3Int, Vector3Int>();
    // anchorPos -> spawned runtime building instance.
    private readonly Dictionary<Vector3Int, BaseBuilding> _buildingInstances = new Dictionary<Vector3Int, BaseBuilding>();

    public IReadOnlyList<BuildingDataSO> AvailableBuildings => _buildingDatabase.Buildings;

    #region Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    #endregion

    #region Query
    public void SelectBuilding(BuildingDataSO data)
    {
        SelectedBuilding = data;
        OnBuildingSelected?.Invoke(data);
    }

    public void ClearSelection()
    {
        SelectedBuilding = null;
    }

    public bool IsOccupied(Vector3Int gridPos) => _occupiedCells.ContainsKey(gridPos);

    public bool IsConstructionComplete(Vector3Int anyPos)
    {
        if (!_occupiedCells.TryGetValue(anyPos, out Vector3Int anchor)) return false;
        if (!_buildings.TryGetValue(anchor, out BuildingSaveData saveData)) return false;
        return saveData.RemainingDays <= 0;
    }
    #endregion

    #region Ghost Preview
    public BuildingPreviewInfo GetPreviewInfo(Vector3Int anchorPos, BuildingDataSO data, int direction)
    {
        BuildingFootprint footprint = BuildingPlacer.GetFootprint(data, direction);
        bool canPlace = CanPlace(anchorPos, footprint, out int baseY);

        var adjustedAnchor = new Vector3Int(anchorPos.x, baseY >= 0 ? baseY : anchorPos.y, anchorPos.z);
        Vector3 spawnPos = CalculateSpawnPos(adjustedAnchor, footprint);
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
        if (!CanPlace(anchorPos, footprint, out _)) return;

        if (info.Sender != null)
        {
            photonView.RPC(nameof(RPC_ConfirmBuild), info.Sender, buildingId);
        }

        photonView.RpcSafe(nameof(RPC_ExecuteBuild), RpcTarget.All,
            buildingId, ax, ay, az, direction);
    }

    [PunRPC]
    private void RPC_ConfirmBuild(string buildingId)
    {
        BuildingDataSO data = _buildingDatabase.GetById(buildingId);
        if (data == null) return;

        OnLocalBuildCostConfirmed?.Invoke(data);
    }

    [PunRPC]
    private void RPC_ExecuteBuild(string buildingId, int ax, int ay, int az, int direction)
    {
        BuildingDataSO data = _buildingDatabase.GetById(buildingId);
        if (data == null) return;

        var request = new BuildingRequest
        {
            Data = data,
            AnchorPos = new Vector3Int(ax, ay, az),
            Direction = direction
        };
        TryBuild(request).Forget();
    }

    [PunRPC]
    private void RPC_RequestRemove(int x, int y, int z, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var pos = new Vector3Int(x, y, z);
        if (!TryGetBuildingInfo(pos, out Vector3Int anchor, out BuildingSaveData saveData, out BuildingDataSO buildingData)) return;
        if (!TryRemoveResolved(anchor, saveData, buildingData)) return;

        photonView.RpcSafe(nameof(RPC_ExecuteRemove), RpcTarget.Others,
            anchor.x, anchor.y, anchor.z);

        if (info.Sender != null)
        {
            photonView.RPC(nameof(RPC_RefundRemove), info.Sender, buildingData.BuildingId);
        }
    }

    [PunRPC]
    private void RPC_ExecuteRemove(int ax, int ay, int az)
    {
        var anchor = new Vector3Int(ax, ay, az);
        TryRemove(anchor, out _);
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
        if (!CanPlace(request.AnchorPos, footprint, out int baseY)) return false;

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

        _buildings[anchor] = saveData;
        MarkOccupiedCells(anchor, footprint);

        string prefabKey = AssetKey.Building.GetKey(request.Data.BuildingId);
        if (!string.IsNullOrEmpty(prefabKey))
        {
            GameObject prefab = await ResourceManager.Instance.LoadAsync<GameObject>(prefabKey);
            if (prefab != null)
            {
                float yRot = footprint.Direction * 90f;
                Vector3 spawnPos = CalculateSpawnPos(anchor, footprint);
                GameObject go = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, yRot, 0f), transform);
                InitializeBuildingInstance(anchor, go, request.Data, saveData);
            }
        }

        return true;
    }

    private bool TryGetBuildingInfo(
        Vector3Int anyPos,
        out Vector3Int anchor,
        out BuildingSaveData saveData,
        out BuildingDataSO buildingData)
    {
        anchor = default;
        saveData = null;
        buildingData = null;

        if (!_occupiedCells.TryGetValue(anyPos, out anchor)) return false;
        if (!_buildings.TryGetValue(anchor, out saveData)) return false;

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

    public Vector3 CalculateSpawnPos(Vector3Int anchorPos, BuildingFootprint footprint)
    {
        int offsetSize = (int)(_gridManager.CellSize * 0.5f);
        Vector3Int elevated = anchorPos + Vector3Int.up * offsetSize;

        float depthCenter = (footprint.Depth - 1) * 0.5f;
        float widthCenter = footprint.WidthOffset + (footprint.Width - 1) * 0.5f;
        float cellSize = _gridManager.CellSize;
        Vector3 centerOffset = new Vector3(
            (footprint.Forward.x * depthCenter + footprint.Right.x * widthCenter) * cellSize,
            0f,
            (footprint.Forward.y * depthCenter + footprint.Right.y * widthCenter) * cellSize
        );

        return _gridManager.GridToWorld(elevated) + centerOffset;
    }

    public bool CanPlace(Vector3Int anchorPos, BuildingFootprint footprint, out int baseY)
    {
        baseY = _gridManager.GetTopY(anchorPos.x, anchorPos.z);
        if (baseY < 0) return false;

        for (int f = 0; f < footprint.Depth; f++)
        {
            for (int r = footprint.WidthOffset; r < footprint.WidthOffset + footprint.Width; r++)
            {
                int cx = anchorPos.x + footprint.Forward.x * f + footprint.Right.x * r;
                int cz = anchorPos.z + footprint.Forward.y * f + footprint.Right.y * r;

                int topY = _gridManager.GetTopY(cx, cz);
                if (topY < 0 || topY != baseY) return false;

                var pos = new Vector3Int(cx, topY, cz);

                if (_occupiedCells.ContainsKey(pos)) return false;

                TerrainCell cell = _gridManager.GetCell(pos);
                if (cell == null) return false;
                if (cell.Data.CellType != ECellType.Dirt) return false;
                if (cell.Data.ObjectType != EGridObjectType.None) return false;
            }
        }

        return true;
    }
    #endregion

    private void MarkOccupiedCells(Vector3Int anchor, BuildingFootprint footprint)
    {
        for (int f = 0; f < footprint.Depth; f++)
        {
            for (int r = footprint.WidthOffset; r < footprint.WidthOffset + footprint.Width; r++)
            {
                int cx = anchor.x + footprint.Forward.x * f + footprint.Right.x * r;
                int cz = anchor.z + footprint.Forward.y * f + footprint.Right.y * r;
                var pos = new Vector3Int(cx, anchor.y, cz);

                _occupiedCells[pos] = anchor;

                TerrainCell cell = _gridManager.GetCell(pos);
                if (cell != null)
                {
                    cell.Data.SetObject(EGridObjectType.Building, int.MaxValue);
                }
            }
        }
    }

    private void InitializeBuildingInstance(
        Vector3Int anchor,
        GameObject instance,
        BuildingDataSO buildingData,
        BuildingSaveData saveData)
    {
        if (instance == null) return;

        BaseBuilding buildingInstance = instance.GetComponent<BaseBuilding>();
        if (buildingInstance == null) return;

        buildingInstance.Initialize(buildingData, saveData);
        _buildingInstances[anchor] = buildingInstance;
    }

    private void DestroyBuildingInstance(Vector3Int anchor)
    {
        if (!_buildingInstances.Remove(anchor, out BaseBuilding buildingInstance)) return;
        if (buildingInstance == null) return;

        Transform instanceRoot = buildingInstance.transform;
        Destroy(instanceRoot.gameObject);
    }

    private bool TryRemoveResolved(Vector3Int anchor, BuildingSaveData saveData, BuildingDataSO buildingData)
    {
        if (saveData == null || buildingData == null) return false;

        BuildingFootprint footprint = BuildingPlacer.GetFootprint(buildingData, saveData.Direction);

        DestroyBuildingInstance(anchor);

        for (int f = 0; f < footprint.Depth; f++)
        {
            for (int r = footprint.WidthOffset; r < footprint.WidthOffset + footprint.Width; r++)
            {
                int cx = anchor.x + footprint.Forward.x * f + footprint.Right.x * r;
                int cz = anchor.z + footprint.Forward.y * f + footprint.Right.y * r;
                var pos = new Vector3Int(cx, anchor.y, cz);

                _occupiedCells.Remove(pos);

                TerrainCell cell = _gridManager.GetCell(pos);
                if (cell != null)
                {
                    cell.Data.RemoveObject();
                }
            }
        }

        _buildings.Remove(anchor);
        return true;
    }

    private void NotifyLocalRemoveRefund(BuildingDataSO buildingData)
    {
        if (buildingData == null) return;
        OnLocalRemoveRefundGranted?.Invoke(buildingData);
    }

    #region Sync
    public void SpawnBuildingNpcs()
    {
        foreach (KeyValuePair<Vector3Int, BuildingSaveData> kvp in _buildings)
        {
            if (kvp.Value.RemainingDays > 0) continue;
            if (!_buildingInstances.TryGetValue(kvp.Key, out BaseBuilding instance) || instance == null) continue;

            BuildingNpcSpawner spawner = instance.GetComponent<BuildingNpcSpawner>();
            if (spawner != null && spawner.HasValidData)
            {
                spawner.SpawnNpc();
            }
        }
    }

    public List<BuildingSaveData> ExportBuildings()
    {
        return new List<BuildingSaveData>(_buildings.Values);
    }

    public async UniTask ImportBuildings(List<BuildingSaveData> list)
    {
        if (list == null) return;

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
            if (!_buildings.TryGetValue(anchor, out BuildingSaveData built)) continue;

            built.RemainingDays = saveData.RemainingDays;
            if (_buildingInstances.TryGetValue(anchor, out BaseBuilding baseBuildingInstance))
            {
                baseBuildingInstance.Initialize(data, built);
            }
        }
    }
    #endregion
}
