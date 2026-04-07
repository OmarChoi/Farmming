using UnityEngine;

public class PlayerTerrainAbility : PlayerAbility
{
    [SerializeField] private float _detectDistance = 2f;
    [SerializeField] private float _footOffset = -2f;

    private Vector3Int _lastGridPos;
    private bool _hasTarget;

    public Vector3Int FrontGridPos => _lastGridPos;

    private TerrainGridManager GridManager => TerrainGridManager.Instance;

    /// 플레이어 앞에 있는 셀을 반환. 없으면 아래 셀을 탐색. 둘 다 없으면 null.
    public TerrainCell GetFrontCell()
    {
        return GetFrontCell(out _);
    }

    /// isBelowFallback: 앞 자리에 셀이 없어서 아래 셀을 반환했으면 true
    public TerrainCell GetFrontCell(out bool isBelowFallback)
    {
        isBelowFallback = false;
        if (GridManager == null) return null;

        var cell = GridManager.GetCell(_lastGridPos);
        if (cell != null) return cell;

        var belowPos = _lastGridPos + Vector3Int.down;
        var belowCell = GridManager.GetCell(belowPos);
        if (belowCell != null) isBelowFallback = true;
        return belowCell;
    }

    private void Update()
    {
        if (!_owner.IsMine) return;
        if (GridManager == null) return;
        UpdateFrontGridPos();
    }

    private void UpdateFrontGridPos()
    {
        Vector3 footPos = _owner.transform.position + Vector3.up * _footOffset;
        Vector3 frontPos = footPos + _owner.transform.forward * _detectDistance;
        _lastGridPos = GridManager.WorldToGrid(frontPos);
        _hasTarget = GridManager.GetCell(_lastGridPos) != null;
    }

    private void OnDrawGizmos()
    {
        if (GridManager == null || _owner == null) return;

        float size = GridManager.CellSize;
        Vector3 worldPos = GridManager.GridToWorld(_lastGridPos);
        Vector3 center = worldPos + new Vector3(0, size * 0.5f, 0);

        Gizmos.color = _hasTarget ? Color.green : Color.red;
        Gizmos.DrawWireCube(center, Vector3.one * size);
    }
}