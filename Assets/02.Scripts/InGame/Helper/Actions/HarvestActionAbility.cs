using System;
using UnityEngine;

// 수확 공룡: IsHarvestable -> 수확
public class HarvestActionAbility : HelperAbility, IHelperAction
{
    public static event Action<Sprite, string, int> OnHarvested;

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
    public void InteractPrimary(TerrainCell cell)
    {
        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null) return;
        if (!farmTile.HasSeed) return;

        CropGrowth cropGrowth = farmTile.CropGrowth;
        if (cropGrowth == null) return;
        if (!cropGrowth.IsHarvestable) return;

        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.Harvest);

        SeedConfig seed = farmTile.PlantedSeed;

        int harvestAmount = UnityEngine.Random.Range(seed.HarvestAmountMin, seed.HarvestAmountMax+1);

        PlayerInventoryAbility inventory = GetInventory();
        if(inventory != null && seed.HarvestItem !=null)
        {
            inventory.AddItem(seed.HarvestItem, harvestAmount);
        }

        if(seed.SeedIcon != null)
        {
            OnHarvested?.Invoke(seed.SeedIcon, seed.SeedName, harvestAmount);
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
