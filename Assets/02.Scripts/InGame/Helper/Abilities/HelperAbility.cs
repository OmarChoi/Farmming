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
}
