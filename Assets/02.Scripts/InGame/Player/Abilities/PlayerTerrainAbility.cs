using UnityEngine;

public class PlayerTerrainAbility : PlayerAbility
{
    [SerializeField] private float _detectDistance = 2f;
    [SerializeField] private float _footOffset = -2f;

    private TerrainGridManager _gridManager;

    private Vector3Int _lastGridPos;
    private bool _hasTarget;

    private void Start()
    {
        _gridManager = TerrainGridManager.Instance;
    }

    /// 플레이어 앞에 있는 셀을 반환. 없으면 아래 셀을 탐색. 둘 다 없으면 null.
    public TerrainCell GetFrontCell()
    {
        return GetFrontCell(out _);
    }

    /// isBelowFallback: 앞 자리에 셀이 없어서 아래 셀을 반환했으면 true
    public TerrainCell GetFrontCell(out bool isBelowFallback)
    {
        isBelowFallback = false;
        var cell = _gridManager.GetCell(_lastGridPos);
        if (cell != null) return cell;

        var belowPos = _lastGridPos + Vector3Int.down;
        var belowCell = _gridManager.GetCell(belowPos);
        if (belowCell != null) isBelowFallback = true;
        return belowCell;
    }

    private void Update()
    {
        if (!_owner.IsMine) return;
        UpdateFrontGridPos();
    }

    private void UpdateFrontGridPos()
    {
        Vector3 footPos = _owner.transform.position + Vector3.up * _footOffset;
        Vector3 frontPos = footPos + _owner.transform.forward * _detectDistance;
        _lastGridPos = _gridManager.WorldToGrid(frontPos);
        _hasTarget = _gridManager.GetCell(_lastGridPos) != null;
    }

    private void OnDrawGizmos()
    {
        if (_gridManager == null || _owner == null) return;

        float size = _gridManager.CellSize;
        Vector3 worldPos = _gridManager.GridToWorld(_lastGridPos);
        Vector3 center = worldPos + new Vector3(0, size * 0.5f, 0);

        Gizmos.color = _hasTarget ? Color.green : Color.red;
        Gizmos.DrawWireCube(center, Vector3.one * size);
    }
}