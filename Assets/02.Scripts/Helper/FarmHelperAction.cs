using UnityEngine;

public class FarmHelperAction : MonoBehaviour, IHelperAction
{
    [SerializeField] private SeedConfig _currentSeed;

    public void SetSeed(SeedConfig seed) => _currentSeed = seed;

    public void Interact(TerrainCell cell)
    {
        FarmTile farmTile = cell.FarmTile;

        if (farmTile != null && farmTile.gameObject.activeSelf)
            farmTile.Interact(_currentSeed);
        else
            cell.TryConvertToFarm();
    }
}