using UnityEngine;

public class FarmHelperActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private SeedItemDataSO _currentSeed;

    public void SetSeed(SeedItemDataSO seed) => _currentSeed = seed;

    public void InteractPrimary(TerrainCell cell)
    {
        _owner.BeginAction();

        FarmTile farmTile = cell.FarmTile;

        if (farmTile != null && farmTile.gameObject.activeSelf)
            farmTile.Interact(_currentSeed);
        else
            cell.TryConvertToFarm();

        _owner.EndAction();
    }

    public void InteractSecondary(TerrainCell cell)
    {
        // TODO: 우클릭 동작 구현
    }
}