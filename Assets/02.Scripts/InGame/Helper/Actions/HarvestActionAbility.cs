using System;
using System.Collections.Generic;
using UnityEngine;

// 수확 공룡: IsHarvestable -> 수확
public class HarvestActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private HarvestItemSO _harvestItem;
    [SerializeField] private int _harvestExperience = 10;

    private HelperAnimationAbility _animAbility;
    private HarvestLegendaryVFXAbility _legendaryVFX;

    private PlayerInventoryAbility GetInventory()
    {
        return _owner.PlayerOwner?.GetAbility<PlayerInventoryAbility>();
    }

    protected override void Awake()
    {
        base.Awake();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
        _legendaryVFX = _owner.GetAbility<HarvestLegendaryVFXAbility>();
    }

    private void OnDisable()
    {
        _owner?.EndAction();
    }

    public void InteractPrimary(TerrainCell cell)
    {
        if (_owner.Grade.CurrentGrade == EHelperGrade.Legendary)
        {
            InteractPrimaryLegendary(cell);
            return;
        }

        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null) return;
        if (!farmTile.HasSeed) return;

        CropGrowth cropGrowth = farmTile.CropGrowth;
        if (cropGrowth == null) return;
        if (!cropGrowth.IsHarvestable) return;

        float cost = _owner.Data.BaseEnergyCost;

        if (_owner.Energy == null || !_owner.Energy.TryConsume(cost))
        {
            return;
        }

        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.Harvest);

        HarvestCell(farmTile, farmTile.PlantedSeed);

        farmTile.Interact();
        _owner.EndAction();
    }

    private void InteractPrimaryLegendary(TerrainCell centerCell)
    {
        float cost = _owner.Data.BaseEnergyCost;
        if (_owner.Energy == null || !_owner.Energy.TryConsume(cost)) return;

        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.Stun);

        Vector3 rightDir = GetRightDirection();
        List<TerrainCell> targetCells = GetLegendaryCells(centerCell);

        var cellHarvests = new List<(TerrainCell, Action)>();
        foreach (TerrainCell targetCell in targetCells)
        {
            TerrainCell captured = targetCell;
            cellHarvests.Add((captured, () => TryHarvestCell(captured)));
        }

        if (_legendaryVFX != null)
        {
            _legendaryVFX.SpawnEffects(centerCell, rightDir, cellHarvests, () =>
            {
                _animAbility?.Play(EHelperAnim.Idle);
                _owner.EndAction();
            });
        }
        else
        {
            foreach (var (_, harvest) in cellHarvests)
                harvest?.Invoke();
            _animAbility?.Play(EHelperAnim.Idle);
            _owner.EndAction();
        }
    }

    private void TryHarvestCell(TerrainCell cell)
    {
        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null || !farmTile.HasSeed) return;

        CropGrowth cropGrowth = farmTile.CropGrowth;
        if (cropGrowth == null || !cropGrowth.IsHarvestable) return;

        HarvestCell(farmTile, farmTile.PlantedSeed);
        farmTile.Interact();
    }

    private void HarvestCell(FarmTile farmTile, SeedItemDataSO seed)
    {
        int harvestAmount = UnityEngine.Random.Range(seed.HarvestAmountMin, seed.HarvestAmountMax + 1);

        PlayerInventoryAbility inventory = GetInventory();
        if (inventory != null && seed.HarvestItem != null)
        {
            QuestReportItemHelper.AddItemAndReportQuest(inventory, seed.HarvestItem, harvestAmount);
            _owner.Experience.Add(_harvestExperience);
        }

        if (seed.Icon != null)
        {
            _harvestItem?.Raise(seed.Icon, seed.DisplayName, harvestAmount);
        }
    }

    private List<TerrainCell> GetLegendaryCells(TerrainCell centerCell)
    {
        var cells = new List<TerrainCell> { centerCell };

        Vector3Int rightOffset = GetGridRightOffset();
        for (int i = 1; i <= 2; i++)
        {
            var rightCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset * i);
            var leftCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition - rightOffset * i);

            if (rightCell != null && rightCell.Data.IsTop) cells.Add(rightCell);
            if (leftCell != null && leftCell.Data.IsTop) cells.Add(leftCell);
        }
        return cells;
    }

    private Vector3Int GetGridRightOffset()
    {
        if (_owner.PlayerOwner == null) return Vector3Int.right;
        Vector3 right = _owner.PlayerOwner.transform.right;
        return new Vector3Int(Mathf.RoundToInt(right.x), 0, Mathf.RoundToInt(right.z));
    }

    private Vector3 GetRightDirection()
    {
        if (_owner.PlayerOwner == null) return Vector3.right;
        return _owner.PlayerOwner.transform.right;
    }

    public void InteractSecondary(TerrainCell cell)
    {
        // TODO: 우클릭 동작 구현
        _owner.EndAction();
    }

    private FarmTile GetFarmTile(TerrainCell cell)
    {
        if (cell == null) return null;
        if (cell.FarmTile != null && cell.FarmTile.gameObject.activeSelf)
        {
            return cell.FarmTile;
        }
        return null;
    }
}
