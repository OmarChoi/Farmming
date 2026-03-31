using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [SerializeField] private TerrainGridManager _gridManager;
    [SerializeField] private BuildingDatabase _buildingDatabase;
    
    // 현재 선택된 건물 데이터.
    public BuildingDataSO SelectedBuilding { get; private set; }
    // UI에서 건물을 선택했을 때 발행되는 이벤트.
    public event Action<BuildingDataSO> OnBuildingSelected;

    // anchorPos -> 건물 메타데이터. 철거 시 크기/방향 복원, 저장/로드 직렬화 대상.
    private readonly Dictionary<Vector3Int, BuildingSaveData> _buildings = new Dictionary<Vector3Int, BuildingSaveData>();
    // 점유 셀 -> 앵커 좌표. 아무 셀에서 건물 전체를 역추적하기 위한 매핑.
    private readonly Dictionary<Vector3Int, Vector3Int> _occupiedCells = new Dictionary<Vector3Int, Vector3Int>();
    // anchorPos -> 건물 프리팹에 붙은 공통 건물 컴포넌트.
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
        if (!_occupiedCells.TryGetValue(anyPos, out var anchor)) return false;
        if (!_buildings.TryGetValue(anchor, out var saveData)) return false;
        return saveData.RemainingDays <= 0;
    }
    #endregion

    #region Ghost Preview

    public BuildingPreviewInfo GetPreviewInfo(Vector3Int anchorPos, BuildingDataSO data, int direction, bool swapped)
    {
        BuildingFootprint footprint = BuildingPlacer.GetFootprint(data, direction, swapped);
        bool canPlace = CanPlace(anchorPos, footprint, out int baseY);

        var adjustedAnchor = new Vector3Int(anchorPos.x, baseY >= 0 ? baseY : anchorPos.y, anchorPos.z);
        Vector3 spawnPos = CalculateSpawnPos(adjustedAnchor, footprint);
        float yRot = footprint.Direction * 90f + (swapped ? 90f : 0f);

        return new BuildingPreviewInfo
        {
            SpawnPosition = spawnPos,
            Rotation = Quaternion.Euler(0f, yRot, 0f),
            CanPlace = canPlace
        };
    }

    #endregion

    #region Build Object
    // 건물 배치
    public async UniTask<bool> TryBuild(BuildingRequest request)
    {
        // 1. Footprint 산출 및 배치 가능 여부 검증
        BuildingFootprint footprint = BuildingPlacer.GetFootprint(request.Data, request.Direction, request.Swapped);
        if (!CanPlace(request.AnchorPos, footprint, out int baseY)) return false;

        // 2. 앵커 좌표 확정 및 SaveData 등록
        var anchor = new Vector3Int(request.AnchorPos.x, baseY, request.AnchorPos.z);

        BuildingSaveData saveData = new BuildingSaveData
        {
            BuildingId = request.Data.BuildingId,
            AnchorX = request.AnchorPos.x,
            AnchorY = baseY,
            AnchorZ = request.AnchorPos.z,
            Direction = request.Direction,
            Swapped = request.Swapped,
            RemainingDays = request.Data.ConstructionDays
        };

        _buildings[anchor] = saveData;

        // 3. 셀 순회: 모든 셀을 점유 마킹
        MarkOccupiedCells(anchor, footprint);

        // 4. 프리팹 스폰 (Footprint 중앙 기준, Pivot은 프리팹에서 설정)
        string prefabKey = AssetKey.Building.GetKey(request.Data.BuildingId);
        if (!string.IsNullOrEmpty(prefabKey))
        {
            var prefab = await ResourceManager.Instance.LoadAsync<GameObject>(prefabKey);
            if (prefab != null)
            {
                float yRot = footprint.Direction * 90f + (request.Swapped ? 90f : 0f);
                Vector3 spawnPos = CalculateSpawnPos(anchor, footprint);
                var go = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, yRot, 0f), transform);
                InitializeBuildingInstance(anchor, go, request.Data, saveData);
            }
        }

        return true;
    }

    // 건물 철거
    public bool TryRemove(Vector3Int anyPos, out BuildingDataSO buildingData)
    {
        buildingData = null;
        // 1. 점유 셀에서 앵커 역추적, SaveData에서 건물 정보 조회
        if (!_occupiedCells.TryGetValue(anyPos, out Vector3Int anchor)) return false;
        if (!_buildings.TryGetValue(anchor, out BuildingSaveData saveData)) return false;

        buildingData = _buildingDatabase.GetById(saveData.BuildingId);
        if (buildingData == null) return false;

        // 2. Footprint 복원 후 전체 점유 셀 역산
        BuildingFootprint footprint = BuildingPlacer.GetFootprint(buildingData, saveData.Direction, saveData.Swapped);

        // 3. 프리팹 파괴
        DestroyBuildingInstance(anchor);

        // 4. 셀 순회: 모든 셀의 점유 마킹 해제
        for (var f = 0; f < footprint.Depth; f++)
        {
            for (var r = footprint.WidthOffset; r < footprint.WidthOffset + footprint.Width; r++)
            {
                int cx = anchor.x + footprint.Forward.x * f + footprint.Right.x * r;
                int cz = anchor.z + footprint.Forward.y * f + footprint.Right.y * r;
                var pos = new Vector3Int(cx, anchor.y, cz);

                _occupiedCells.Remove(pos);

                var cell = _gridManager.GetCell(pos);
                if (cell != null)
                {
                    cell.Data.RemoveObject();
                }
            }
        }

        // 5. 건물 데이터 제거
        _buildings.Remove(anchor);
        return true;
    }

    // Footprint 중앙 기준 스폰 위치 계산 (Ghost 위치 갱신과 실제 건설 공용)
    public Vector3 CalculateSpawnPos(Vector3Int anchorPos, BuildingFootprint footprint)
    {
        int offsetSize = (int)(_gridManager.CellSize * 0.5f);
        var elevated = anchorPos + Vector3Int.up * offsetSize;

        // Depth 방향 중앙 오프셋 계산 (Width는 이미 중앙 정렬)
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

    // 배치 가능 조건: 평탄 지형 + 빈 Dirt 셀 + 미점유
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

                var cell = _gridManager.GetCell(pos);
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

                var cell = _gridManager.GetCell(pos);
                if (cell != null)
                {
                    cell.Data.SetObject(EGridObjectType.Building, int.MaxValue);
                }
            }
        }
    }

    private void InitializeBuildingInstance(Vector3Int anchor, GameObject instance, BuildingDataSO buildingData, BuildingSaveData saveData)
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
}
