using UnityEngine;

/// <summary>
/// 건물 배치 규칙 판정과 푸트프린트 기반 좌표/상태 계산을 담당한다.
/// 그리드와 레지스트리 양쪽을 읽고, 점유 적용/해제를 수행한다.
/// </summary>
public class BuildingPlacementService
{
    private readonly TerrainGridManager _gridManager;
    private readonly BuildingRegistry _registry;

    public BuildingPlacementService(TerrainGridManager gridManager, BuildingRegistry registry)
    {
        _gridManager = gridManager;
        _registry = registry;
    }

    // 주어진 앵커에 건물을 배치할 수 있는지 검사한다. 푸트프린트 전체 셀이 동일 높이, Dirt 타입, 비점유 상태여야 한다.
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

                if (_registry.IsOccupied(pos)) return false;

                TerrainCell cell = _gridManager.GetCell(pos);
                if (cell == null) return false;
                if (cell.Data.CellType != ECellType.Dirt) return false;
                if (cell.Data.ObjectType != EGridObjectType.None) return false;
            }
        }

        return true;
    }

    // 앵커 셀 기준으로 Footprint 중심의 월드 스폰 좌표를 계산한다.
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

    // Footprint 영역의 셀을 점유 상태로 표시한다(레지스트리 + 그리드).
    public void MarkOccupied(Vector3Int anchor, BuildingFootprint footprint)
    {
        for (int f = 0; f < footprint.Depth; f++)
        {
            for (int r = footprint.WidthOffset; r < footprint.WidthOffset + footprint.Width; r++)
            {
                int cx = anchor.x + footprint.Forward.x * f + footprint.Right.x * r;
                int cz = anchor.z + footprint.Forward.y * f + footprint.Right.y * r;
                var pos = new Vector3Int(cx, anchor.y, cz);

                _registry.MarkCell(pos, anchor);

                TerrainCell cell = _gridManager.GetCell(pos);
                if (cell != null)
                {
                    cell.Data.SetObject(EGridObjectType.Building, int.MaxValue);
                }
            }
        }
    }

    // Footprint 영역의 점유 상태를 해제한다(레지스트리 + 그리드).
    public void ClearOccupied(Vector3Int anchor, BuildingFootprint footprint)
    {
        for (int f = 0; f < footprint.Depth; f++)
        {
            for (int r = footprint.WidthOffset; r < footprint.WidthOffset + footprint.Width; r++)
            {
                int cx = anchor.x + footprint.Forward.x * f + footprint.Right.x * r;
                int cz = anchor.z + footprint.Forward.y * f + footprint.Right.y * r;
                var pos = new Vector3Int(cx, anchor.y, cz);

                _registry.UnmarkCell(pos);

                TerrainCell cell = _gridManager.GetCell(pos);
                if (cell != null)
                {
                    cell.Data.RemoveObject();
                }
            }
        }
    }
}
