using System.Collections.Generic;
using UnityEngine;

public abstract class HelperAbility : MonoBehaviour
{
    protected HelperController _owner { get; private set; }

    protected virtual void Awake()
    {
        _owner = GetComponent<HelperController>();
    }

    protected TerrainCell GetInteractableCell(TerrainCell cell)
    {
        if (TerrainGridManager.Instance == null) return null;
        return TerrainGridManager.Instance.IsCellAvailableForInteraction(cell) ? cell : null;
    }

    protected TerrainCell GetGridInteractableCell(Vector3Int gridPos, bool allowBelowFallback = false)
    {
        if (TerrainGridManager.Instance == null) return null;
        return TerrainGridManager.Instance.GetInteractionCell(gridPos, allowBelowFallback, out _);
    }

    protected TerrainCell[] GetHorizontalAdjacentCells(TerrainCell centerCell)
    {
        if (centerCell == null) return new TerrainCell[0];
        if (TerrainGridManager.Instance == null) return new TerrainCell[0];
        if (_owner?.PlayerOwner == null) return new TerrainCell[0];

        Vector3 playerRight = _owner.PlayerOwner.transform.right;
        Vector3Int rightOffset = new Vector3Int(
            Mathf.RoundToInt(playerRight.x),
            0,
            Mathf.RoundToInt(playerRight.z));

        if (rightOffset == Vector3Int.zero)
            rightOffset = Vector3Int.right;

        Vector3Int centerGrid = centerCell.GridPosition;
        List<TerrainCell> result = new List<TerrainCell>();

        TerrainCell rightCell = GetGridInteractableCell(centerGrid + rightOffset);
        TerrainCell leftCell = GetGridInteractableCell(centerGrid - rightOffset);

        if (rightCell != null) result.Add(rightCell);
        if (leftCell != null) result.Add(leftCell);

        return result.ToArray();
    }
}
