using UnityEngine;

public class FarmHelperActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private SeedConfig _currentSeed;

    public void SetSeed(SeedConfig seed) => _currentSeed = seed;

    public void InteractPrimary(TerrainCell cell)
    {
        FarmTile farmTile = cell.FarmTile;

        if (farmTile != null && farmTile.gameObject.activeSelf)
            farmTile.Interact(_currentSeed);
        else
            cell.TryConvertToFarm();
    }

    public void InteractSecondary(TerrainCell cell)
    {
        // TODO: 우클릭 동작 구현
    }
}