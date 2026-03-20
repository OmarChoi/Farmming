using UnityEngine;

public static class ResourcePlacer
{
    public static void TryPlace(
        TerrainGridData gridData,
        Vector3Int topPos,
        ResourceEntry[] resources,
        System.Random rng)
    {
        if (resources == null || resources.Length == 0) return;

        var cell = gridData.GetCell(topPos);
        if (cell == null || cell.ObjectType != EGridObjectType.None) return;

        foreach (var res in resources)
        {
            if (rng.NextDouble() < res.SpawnChance)
            {
                cell.SetObject(res.Type, res.Level);
                return;
            }
        }
    }
}