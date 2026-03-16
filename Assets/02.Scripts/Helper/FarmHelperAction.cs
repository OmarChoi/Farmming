using UnityEngine;

public class FarmHelperAction : MonoBehaviour, IHelperAction
{
    [SerializeField] private SeedConfig _currentSeed;

    public void SetSeed(SeedConfig seed) => _currentSeed = seed;

    public void Interact(TerrainCell cell)
    {
        FarmTile farmTile = cell.GetComponentInChildren<FarmTile>();

        if (farmTile != null && farmTile.gameObject.activeSelf)
        {
            farmTile.Interact(_currentSeed);
        }
        else
        {
            if (cell.TryConvertToFarm())
                Debug.Log("경작지로 전환");
            else
                Debug.Log("경작할 수 없는 땅");
        }
    }
}