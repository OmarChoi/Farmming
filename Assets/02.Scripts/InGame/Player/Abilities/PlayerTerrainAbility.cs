using UnityEngine;

public class PlayerTerrainAbility : PlayerAbility
{
    [SerializeField] private TerrainGridManager _gridManager;
    [SerializeField] private float _detectDistance = 2f;
    [SerializeField] private float _footOffset = -2f;

    private Vector3Int _lastGridPos;
    private bool _hasTarget;

    /// 플레이어 앞에 있는 셀을 반환. 없으면 null.
    public TerrainCell GetFrontCell()
    {
        return _gridManager.GetCell(_lastGridPos);
    }

    private void Update()
    {
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