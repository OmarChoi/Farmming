using System.Collections.Generic;
using UnityEngine;

public class PlayerGridIndicatorAbility : PlayerAbility
{
    [SerializeField] private GameObject _indicatorPrefab;
    [SerializeField] private float _heightOffset = 0.05f;

    private PlayerHelperInteractionAbility _helperInteraction;
    private PlayerTerrainAbility _terrainAbility;
    private TerrainGridManager GridManager => TerrainGridManager.Instance;

    private readonly List<GameObject> _indicatorPool = new();
    private readonly List<Vector3Int> _positionBuffer = new();

    private int _activeCount;

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
        TerrainCell centerCell = isGroundHelper
            ? _terrainAbility.GetFrontCellForGroundHelper()
            : _terrainAbility.GetFrontCell();
        if (centerCell == null)
        {
            HideAll();
            return;
        }

        GetIndicatorPositions(centerCell.GridPosition, helper);
        List<Vector3Int> positions = _positionBuffer;

        _activeCount = 0;
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3Int gridPos = positions[i];
            TerrainCell cell = GridManager.GetInteractionCell(gridPos, false, out _);

            Vector3 worldPos = GridManager.GridToWorld(gridPos);
            worldPos.y += GridManager.CellSize * 0.5f + _heightOffset;

            bool canInteract = cell != null && action.CanInteractPrimary(cell);
            if (!canInteract) continue;

            GameObject indicator = GetIndicator(_activeCount);
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

    private void GetIndicatorPositions(Vector3Int centerGridPos, HelperController helper)
    {
        _positionBuffer.Clear();
        _positionBuffer.Add(centerGridPos);

        int range = helper.Grade.GetRange();
        int extension = range - 1;
        if (extension <= 0) return;

        Vector3Int rightOffset = GetGridRightOffset();
        for (int i = 1; i <= extension; i++)
        {
            _positionBuffer.Add(centerGridPos + rightOffset * i);
            _positionBuffer.Add(centerGridPos - rightOffset * i);
        }
    }

    private Vector3Int GetGridRightOffset()
    {
        Vector3 right = _owner.transform.right;
        return new Vector3Int(Mathf.RoundToInt(right.x), 0, Mathf.RoundToInt(right.z));
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

    private void HideAll()
    {
        for (int i = 0; i < _indicatorPool.Count; i++)
        {
            _indicatorPool[i].SetActive(false);
        }

        _activeCount = 0;
    }
}
