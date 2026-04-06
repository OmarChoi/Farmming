using System;
using UnityEngine;

// 수확 공룡: IsHarvestable -> 수확
public class HarvestActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private HarvestItemSO _harvestItem;
    [SerializeField] private int _harvestExperience = 10;
    private PlayerInventoryAbility GetInventory()
    {
        return _owner.PlayerOwner?.GetAbility<PlayerInventoryAbility>();
    }

    private HelperAnimationAbility _animAbility;

    protected override void Awake()
    {
        base.Awake();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
    }

    private void OnDisable()
    {
        _owner?.EndAction();
    }

    public void InteractPrimary(TerrainCell cell)
    {
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

        SeedItemDataSO seed = farmTile.PlantedSeed;

        int harvestAmount = UnityEngine.Random.Range(seed.HarvestAmountMin, seed.HarvestAmountMax+1);

        PlayerInventoryAbility inventory = GetInventory();
        if(inventory != null && seed.HarvestItem !=null)
        {
            QuestReportItemHelper.AddItemAndReportQuest(inventory, seed.HarvestItem, harvestAmount);
            _owner.Experience.Add(_harvestExperience);
        }

        if(seed.Icon != null)
        {
            _harvestItem?.Raise(seed.Icon, seed.DisplayName, harvestAmount);
        }

        farmTile.Interact();
        _owner.EndAction();
    }

    public void InteractSecondary(TerrainCell cell)
    {
        // TODO: 우클릭 동작 구현
        _owner.EndAction();
    }

    private FarmTile GetFarmTile(TerrainCell cell)
    {
        if (cell == null)
        {
            return null;
        }
        if (cell.FarmTile != null && cell.FarmTile.gameObject.activeSelf)
        {
            return cell.FarmTile;
        }
        return null;
    }
}
