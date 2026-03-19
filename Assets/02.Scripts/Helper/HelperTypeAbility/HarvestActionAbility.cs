using UnityEngine;

// 수확 공룡: IsHarvestable -> 수확
public class HarvestActionAbility : HelperAbility, IHelperAction
{
    private PlayerInventoryAbility GetInventory()
    {
        return _owner.PlayerOwner?.GetAbility<PlayerInventoryAbility>();
    }

    private HelperAnimationAbility _animAbility;

    private void Start()
    {
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
    }
    public void InteractPrimary(TerrainCell cell)
    {
        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null)
        {
            return;
        }

        if(!farmTile.HasSeed)
        {
            return;
        }

        CropGrowth cropGrowth = farmTile.GetComponent<CropGrowth>();
        if (cropGrowth == null)
        {
            return;
        }
        if(!cropGrowth.IsHarvestable)
        {
            return;
        }

        _animAbility?.Play(EHelperAnim.Harvest);

        SeedConfig seed = farmTile.PlantedSeed;

        int harvestAmount = Random.Range(seed.HarvestAmountMin, seed.HarvestAmountMax+1);

        PlayerInventoryAbility inventory = GetInventory();
        if(inventory != null && seed.HarvestItem !=null)
        {
            inventory.AddItem(seed.HarvestItem, harvestAmount);
        }

        if(seed.SeedIcon != null)
        {
            HarvestNotificationManager.Instance?.Show(seed.SeedIcon,seed.SeedName,harvestAmount);
        }

        farmTile.Interact();
    }
    
    public void InteractSecondary(TerrainCell cell)
    {
        // TODO: 우클릭 동작 구현
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
