using System.Collections.Generic;
using UnityEngine;

public class PlayerGridIndicatorAbility : PlayerAbility
{
    private readonly struct CellSurfaceCache
    {
        public CellSurfaceCache(bool hasSurface, float surfaceY)
        {
            HasSurface = hasSurface;
            SurfaceY = surfaceY;
        }

        public bool HasSurface { get; }
        public float SurfaceY { get; }
    }

    [SerializeField] private GameObject _indicatorPrefab;
    [SerializeField] private float _heightOffset = 0.05f;

    private PlayerHelperInteractionAbility _helperInteraction;
    private PlayerTerrainAbility _terrainAbility;
    private TerrainGridManager GridManager => TerrainGridManager.Instance;

    private readonly List<GameObject> _indicatorPool = new();
    private readonly List<Vector3Int> _positionBuffer = new();
    private readonly List<TerrainCell> _cellBuffer = new();
    private readonly Dictionary<TerrainCell, CellSurfaceCache> _cellSurfaceCache = new();

    private int _activeCount;
    private bool _hasIndicatorVisualBaseOffset;
    private float _indicatorVisualBaseOffset;

    protected override void Awake()
    {
        base.Awake();
        _helperInteraction = _owner.GetAbility<PlayerHelperInteractionAbility>();
        _terrainAbility = _owner.GetAbility<PlayerTerrainAbility>();
    }

    private void LateUpdate()
    {
        if (!_owner.IsMine) return;

        HelperController helper = _helperInteraction?.CurrentHelper;

        if (helper == null || helper.State != EHelperState.Equipped || GridManager == null || _indicatorPrefab == null)
        {
            HideAll();
            return;
        }

        IHelperAction action = helper.GetAbility<HelperInteractionAbility>()?.Action;
        if (action == null)
        {
            HideAll();
            return;
        }

        UpdateIndicators(helper, action);
    }

    private void UpdateIndicators(HelperController helper, IHelperAction action)
    {
        bool isGroundHelper = helper.GetAbility<GroundActionAbility>() != null;
        bool isBelowFallback = false;
        TerrainCell centerCell = isGroundHelper
            ? _terrainAbility.GetFrontCellForGroundHelper()
            : _terrainAbility.GetFrontCell(out isBelowFallback);

        if (!isGroundHelper && isBelowFallback)
        {
            HideAll();
            return;
        }

        if (centerCell == null)
        {
            HideAll();
            return;
        }

        BuildPrimaryIndicatorCells(helper, action, centerCell);
        List<TerrainCell> targetCells = _cellBuffer;

        _activeCount = 0;
        for (int i = 0; i < targetCells.Count; i++)
        {
            TerrainCell cell = targetCells[i];
            if (cell == null)
                continue;

            Vector3 worldPos = GetIndicatorWorldPosition(cell);

            GameObject indicator = GetIndicator(_activeCount);
            worldPos.y -= GetIndicatorVisualBaseOffset(indicator);
            indicator.transform.position = worldPos;
            indicator.transform.rotation = Quaternion.identity;
            indicator.SetActive(true);

            _activeCount++;
        }

        for (int i = _activeCount; i < _indicatorPool.Count; i++)
        {
            _indicatorPool[i].SetActive(false);
        }
    }

    private void BuildPrimaryIndicatorCells(HelperController helper, IHelperAction action, TerrainCell centerCell)
    {
        _cellBuffer.Clear();
        if (helper == null || action == null || centerCell == null || GridManager == null)
            return;

        int extension = GetPrimaryIndicatorExtension(helper);
        BuildCandidatePositions(centerCell.GridPosition, extension);

        for (int i = 0; i < _positionBuffer.Count; i++)
        {
            TerrainCell cell = GridManager.GetInteractionCell(_positionBuffer[i], false, out _);
            if (!ShouldShowPrimaryIndicatorForCell(helper, action, centerCell, cell))
                continue;

            if (!_cellBuffer.Contains(cell))
                _cellBuffer.Add(cell);
        }
    }

    private int GetPrimaryIndicatorExtension(HelperController helper)
    {
        if (helper == null)
            return 0;

        if (helper.GetAbility<GroundActionAbility>() != null)
            return 0;

        if (helper.GetAbility<WoodCuttingMineActionAbility>() != null)
        {
            RangeBoostEffect rangeBoostEffect = helper.GetAbility<RangeBoostEffect>();
            return rangeBoostEffect != null && rangeBoostEffect.IsActive ? 1 : 0;
        }

        return Mathf.Max(0, helper.Grade.GetRange() - 1);
    }

    private void BuildCandidatePositions(Vector3Int centerGridPos, int extension)
    {
        _positionBuffer.Clear();
        _positionBuffer.Add(centerGridPos);

        if (extension <= 0)
            return;

        Vector3Int rightOffset = GetGridRightOffset();
        for (int i = 1; i <= extension; i++)
        {
            _positionBuffer.Add(centerGridPos + rightOffset * i);
            _positionBuffer.Add(centerGridPos - rightOffset * i);
        }
    }

    private bool ShouldShowPrimaryIndicatorForCell(HelperController helper, IHelperAction action, TerrainCell centerCell, TerrainCell cell)
    {
        if (helper == null || action == null || cell == null)
            return false;

        if (helper.GetAbility<WaterActionAbility>() != null)
            return CanShowWaterPrimaryIndicator(helper, cell);

        if (helper.GetAbility<SowActionAbility>() != null)
            return CanShowSowPrimaryIndicator(cell);

        return action.CanInteractPrimary(cell);
    }

    private bool CanShowWaterPrimaryIndicator(HelperController helper, TerrainCell cell)
    {
        if (cell == null)
            return false;

        if (cell.CurrentObject != null)
            return false;

        if (helper != null && helper.Grade.CurrentGrade == EHelperGrade.Normal)
        {
            TerrainCell frontCell = _terrainAbility.GetFrontCell(out bool isBelowFallback);
            return !isBelowFallback && frontCell != null && cell == frontCell;
        }

        return true;
    }

    private bool CanShowSowPrimaryIndicator(TerrainCell cell)
    {
        if (cell == null || cell.Data == null)
            return false;

        if (cell.Data.CellType != ECellType.Dirt)
            return false;

        return cell.Data.ObjectType == EGridObjectType.None
               || cell.Data.ObjectType == EGridObjectType.FarmLand;
    }

    private Vector3 GetIndicatorWorldPosition(TerrainCell cell)
    {
        if (cell == null)
            return Vector3.zero;

        if (TryGetCellSurfaceY(cell, out float surfaceY))
        {
            Vector3 position = cell.transform.position;
            position.y = surfaceY + _heightOffset;
            return position;
        }

        Vector3 fallback = GridManager.GridToWorld(cell.GridPosition);
        fallback.y += GridManager.CellSize * 0.5f + _heightOffset;
        return fallback;
    }

    private bool TryGetCellSurfaceY(TerrainCell cell, out float surfaceY)
    {
        surfaceY = 0f;
        if (cell == null)
            return false;

        if (_cellSurfaceCache.TryGetValue(cell, out CellSurfaceCache cached))
        {
            surfaceY = cached.SurfaceY;
            return cached.HasSurface;
        }

        Collider[] colliders = cell.GetComponentsInChildren<Collider>(true);
        bool found = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (!IsCellVisualComponent(cell, collider.transform))
                continue;

            surfaceY = found ? Mathf.Max(surfaceY, collider.bounds.max.y) : collider.bounds.max.y;
            found = true;
        }

        if (found)
            return true;

        Renderer[] renderers = cell.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!IsCellVisualComponent(cell, renderer.transform))
                continue;

            surfaceY = found ? Mathf.Max(surfaceY, renderer.bounds.max.y) : renderer.bounds.max.y;
            found = true;
        }

        _cellSurfaceCache[cell] = new CellSurfaceCache(found, surfaceY);
        return found;
    }

    private static bool IsCellVisualComponent(TerrainCell cell, Transform target)
    {
        if (cell == null || target == null)
            return false;

        if (cell.CurrentObject != null && target.IsChildOf(cell.CurrentObject.transform))
            return false;

        if (cell.FarmTile != null && target.IsChildOf(cell.FarmTile.transform))
            return false;

        return true;
    }

    private Vector3Int GetGridRightOffset()
    {
        Vector3 right = _owner.transform.right;
        Vector3Int rightOffset = new Vector3Int(Mathf.RoundToInt(right.x), 0, Mathf.RoundToInt(right.z));
        return rightOffset == Vector3Int.zero ? Vector3Int.right : rightOffset;
    }

    private GameObject GetIndicator(int index)
    {
        while (_indicatorPool.Count <= index)
        {
            GameObject indicator = Instantiate(_indicatorPrefab, transform);
            indicator.SetActive(false);
            _indicatorPool.Add(indicator);
        }

        return _indicatorPool[index];
    }

    private float GetIndicatorVisualBaseOffset(GameObject indicator)
    {
        if (_hasIndicatorVisualBaseOffset)
            return _indicatorVisualBaseOffset;

        _hasIndicatorVisualBaseOffset = true;
        _indicatorVisualBaseOffset = 0f;

        if (indicator == null)
            return _indicatorVisualBaseOffset;

        Renderer[] renderers = indicator.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            float offset = renderer.bounds.min.y - indicator.transform.position.y;
            _indicatorVisualBaseOffset = found ? Mathf.Min(_indicatorVisualBaseOffset, offset) : offset;
            found = true;
        }

        return _indicatorVisualBaseOffset;
    }

    private void HideAll()
    {
        for (int i = 0; i < _indicatorPool.Count; i++)
        {
            _indicatorPool[i].SetActive(false);
        }

        _activeCount = 0;
    }

    private void OnDisable()
    {
        _cellSurfaceCache.Clear();
    }
}
