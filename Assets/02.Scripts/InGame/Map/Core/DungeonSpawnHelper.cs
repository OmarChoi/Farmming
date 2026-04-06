using System.Collections.Generic;
using UnityEngine;

public static class DungeonSpawnHelper
{
    public static Vector3 FindSpawnPosition(int floor)
    {
        DungeonMapConfig config = MapManager.Instance.GetDungeonConfig(floor);
        var gridManager = MapManager.Instance.GridManager;
        var gridData = gridManager.GetGridData();

        if (config != null && config.SpawnMode == DungeonSpawnMode.TopCellWithAllowedTile)
        {
            if (TryFindAllowedTileSpawnPosition(gridData, gridManager, config, out Vector3 allowedSpawn))
                return allowedSpawn;

            Debug.LogWarning("[DungeonSpawnHelper] No valid allowed-tile spawn found. Falling back to center-top spawn.");
        }

        return FindCenterTopSpawnPosition(gridData, gridManager);
    }

    public static void SpawnChests(int floor, int seed)
    {
        DungeonMapConfig config = MapManager.Instance.GetDungeonConfig(floor);
        if (config == null || config.ChestPrefab == null || config.ChestCount <= 0)
            return;

        var gridManager = MapManager.Instance.GridManager;
        var candidates = new List<TerrainCell>();

        foreach (var kvp in gridManager.Cells)
        {
            var cell = kvp.Value;
            if (cell == null) continue;
            if (!cell.Data.IsTop) continue;
            if (cell.Data.ObjectType != EGridObjectType.None) continue;
            if (cell.Data.TileType == ETileType.Dungeon3Lava) continue;
            if (!cell.HasObjectPoint) continue;

            candidates.Add(cell);
        }

        if (candidates.Count == 0) return;

        var rng = new System.Random(seed);
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        int count = Mathf.Min(config.ChestCount, candidates.Count);
        var lootTables = config.ChestLootTables;
        bool hasLoot = lootTables != null && lootTables.Length > 0;

        for (int i = 0; i < count; i++)
        {
            candidates[i].SpawnObject(config.ChestPrefab, EGridObjectType.Chest);

            var chest = candidates[i].CurrentObject.GetComponent<DungeonChest>();
            if (chest != null && hasLoot)
                chest.Init(lootTables[i % lootTables.Length]);
        }
    }

    private static Vector3 FindCenterTopSpawnPosition(TerrainGridData gridData, TerrainGridManager gridManager)
    {
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minZ = int.MaxValue;
        int maxZ = int.MinValue;

        foreach (var pos in gridData.Cells.Keys)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.z < minZ) minZ = pos.z;
            if (pos.z > maxZ) maxZ = pos.z;
        }

        int cx = (minX + maxX) / 2;
        int cz = (minZ + maxZ) / 2;

        for (int y = 20; y >= 0; y--)
        {
            if (gridData.HasCell(new Vector3Int(cx, y, cz)))
                return gridManager.GridToWorld(new Vector3Int(cx, y + 1, cz));
        }

        return Vector3.zero;
    }

    private static bool TryFindAllowedTileSpawnPosition(TerrainGridData gridData, TerrainGridManager gridManager, DungeonMapConfig config, out Vector3 spawnPosition)
    {
        if (config.AllowedSpawnTiles == null || config.AllowedSpawnTiles.Length == 0)
        {
            spawnPosition = default;
            return false;
        }

        int centerX = config.Width / 2;
        int centerZ = config.Height / 2;
        bool found = false;
        Vector3Int bestCell = default;
        int bestDistance = int.MaxValue;

        foreach (var kvp in gridData.Cells)
        {
            Vector3Int pos = kvp.Key;
            TerrainCellData cell = kvp.Value;

            if (!cell.IsTop)
                continue;

            if (!IsAllowedSpawnTile(cell.TileType, config.AllowedSpawnTiles))
                continue;

            int distance = Mathf.Abs(pos.x - centerX) + Mathf.Abs(pos.z - centerZ);
            if (!found || distance < bestDistance)
            {
                found = true;
                bestCell = pos;
                bestDistance = distance;
            }
        }

        if (!found)
        {
            spawnPosition = default;
            return false;
        }

        spawnPosition = gridManager.GridToWorld(bestCell + Vector3Int.up);
        return true;
    }

    private static bool IsAllowedSpawnTile(ETileType tileType, ETileType[] allowedTiles)
    {
        foreach (ETileType allowedTile in allowedTiles)
        {
            if (tileType == allowedTile)
                return true;
        }

        return false;
    }
}