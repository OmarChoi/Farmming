using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [SerializeField] private TerrainGridManager _gridManager;
    [SerializeField] private BuildingDatabase _buildingDatabase;

    // anchorPos -> 건물 메타데이터. 철거 시 크기/방향 복원, 저장/로드 직렬화 대상.
    private readonly Dictionary<Vector3Int, BuildingSaveData> _buildings = new Dictionary<Vector3Int, BuildingSaveData>();
    // 점유 셀 -> 앵커 좌표. 아무 셀에서 건물 전체를 역추적하기 위한 매핑.
    private readonly Dictionary<Vector3Int, Vector3Int> _occupiedCells = new Dictionary<Vector3Int, Vector3Int>();
    // anchorPos -> 스폰된 건물 프리팹 인스턴스.
    private readonly Dictionary<Vector3Int, GameObject> _instances = new Dictionary<Vector3Int, GameObject>();

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

    private void Start()
    {
        DayNightCycle.Instance.OnMorningStart += AdvanceDay;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnMorningStart -= AdvanceDay;
        }
    }

    #endregion

    #region Query

    public IReadOnlyList<BuildingDataSO> AvailableBuildings => _buildingDatabase.Buildings;

    /// <summary>UI에서 건물을 선택했을 때 발행되는 이벤트. PlayerBuildingAbility가 구독한다.</summary>
    public event Action<BuildingDataSO> OnBuildingSelected;

    public void SelectBuilding(BuildingDataSO data)
    {
        OnBuildingSelected?.Invoke(data);
    }

    public bool IsOccupied(Vector3Int gridPos) => _occupiedCells.ContainsKey(gridPos);

    public bool IsConstructionComplete(Vector3Int anyPos)
    {
        if (!_occupiedCells.TryGetValue(anyPos, out var anchor)) return false;
        if (!_buildings.TryGetValue(anchor, out var saveData)) return false;
        return saveData.RemainingDays <= 0;
    }

    #endregion

    #region Construction Progress

    // 아침마다 호출. 건설 중인 건물의 남은 일수 차감.
    private void AdvanceDay()
    {
        foreach (KeyValuePair<Vector3Int, BuildingSaveData> kvp in _buildings)
        {
            if (kvp.Value.RemainingDays <= 0) continue;
            kvp.Value.RemainingDays--;
            // todo. RemainingDays에 따른 건설 진행률 표시
            // todo. 건설 종료에 따른 NPC에게 알림 설정 
        }
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

        _buildings[anchor] = new BuildingSaveData
        {
            BuildingId = request.Data.BuildingId,
            AnchorX = request.AnchorPos.x,
            AnchorY = baseY,
            AnchorZ = request.AnchorPos.z,
            Direction = request.Direction,
            Swapped = request.Swapped,
            RemainingDays = request.Data.ConstructionDays
        };

        // 3. 셀 순회: 모든 셀을 점유 마킹
        for (int f = 0; f < footprint.Depth; f++)
        {
            for (int r = footprint.WidthOffset; r < footprint.WidthOffset + footprint.Width; r++)
            {
                int cx = request.AnchorPos.x + footprint.Forward.x * f + footprint.Right.x * r;
                int cz = request.AnchorPos.z + footprint.Forward.y * f + footprint.Right.y * r;
                var pos = new Vector3Int(cx, baseY, cz);

                _occupiedCells[pos] = anchor;

                var cell = _gridManager.GetCell(pos);
                if (cell != null)
                {
                    cell.Data.SetObject(EGridObjectType.Building, int.MaxValue);
                }
            }
        }

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
                _instances[anchor] = go;
            }
        }

        return true;
    }

    // 건물 철거
    public bool TryRemove(Vector3Int anyPos)
    {
        // 1. 점유 셀에서 앵커 역추적, SaveData에서 건물 정보 조회
        if (!_occupiedCells.TryGetValue(anyPos, out Vector3Int anchor)) return false;
        if (!_buildings.TryGetValue(anchor, out BuildingSaveData saveData)) return false;

        BuildingDataSO buildingData = _buildingDatabase.GetById(saveData.BuildingId);
        if (buildingData == null) return false;

        // 2. Footprint 복원 후 전체 점유 셀 역산
        BuildingFootprint footprint = BuildingPlacer.GetFootprint(buildingData, saveData.Direction, saveData.Swapped);

        // 3. 프리팹 파괴
        if (_instances.TryGetValue(anchor, out var go))
        {
            Destroy(go);
            _instances.Remove(anchor);
        }

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
        float cellSize = _gridManager.CellSize;
        Vector3 centerOffset = new Vector3(
            footprint.Forward.x * depthCenter * cellSize,
            0f,
            footprint.Forward.y * depthCenter * cellSize
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
}
