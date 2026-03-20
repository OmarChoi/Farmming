using System.Collections.Generic;
using UnityEngine;

public class CaveMapGenerator : IMapGenerator
{
    private const int WallNeighborThreshold = 4;
    private const int RegionSampleLimit = 50;

    private static readonly int[] DirX = { 1, -1, 0, 0 };
    private static readonly int[] DirZ = { 0, 0, 1, -1 };

    public MapGenerationResult Generate(MapConfig config, int seed)
    {
        if (config is not DungeonMapConfig dc)
        {
            Debug.LogError("CaveMapGenerator requires a DungeonMapConfig.");
            return new MapGenerationResult
            {
                GridData = new TerrainGridData(),
                SpawnPoint = Vector3Int.zero
            };
        }

        var gridData = new TerrainGridData();
        var rng = new System.Random(seed);

        int w = config.Width;
        int h = config.Height;

        bool[,] wallMap = BuildWallMap(w, h, dc.CaveFillPercent, dc.CaveSmoothIterations, rng);
        ETileType[,] tileMap = BuildTileMap(dc, rng);
        int[,] floorHeightMap = BuildFloorHeightMap(config, rng);

        FillTerrain(gridData, dc, wallMap, tileMap, floorHeightMap, rng);

        return new MapGenerationResult
        {
            GridData = gridData,
            SpawnPoint = FindOpenSpawn(wallMap, gridData, dc)
        };
    }

    #region Wall Generation (Cellular Automata)

    private bool[,] BuildWallMap(int w, int h, float fillPercent, int smoothIterations, System.Random rng)
    {
        var map = new bool[w, h];

        for (int x = 0; x < w; x++)
            for (int z = 0; z < h; z++)
                map[x, z] = rng.NextDouble() < fillPercent;

        for (int i = 0; i < smoothIterations; i++)
            map = SmoothWalls(map, w, h);

        SealEdges(map, w, h);
        EnsureConnectivity(map, w, h);

        return map;
    }

    private bool[,] SmoothWalls(bool[,] map, int w, int h)
    {
        var next = new bool[w, h];
        for (int x = 0; x < w; x++)
            for (int z = 0; z < h; z++)
                next[x, z] = CountWallNeighbors(map, x, z, w, h) > WallNeighborThreshold;
        return next;
    }

    private int CountWallNeighbors(bool[,] map, int x, int z, int w, int h)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                int nx = x + dx;
                int nz = z + dz;
                if (nx < 0 || nx >= w || nz < 0 || nz >= h)
                    count++;
                else if (map[nx, nz])
                    count++;
            }
        }
        return count;
    }

    private void SealEdges(bool[,] map, int w, int h)
    {
        for (int x = 0; x < w; x++)
        {
            map[x, 0] = true;
            map[x, h - 1] = true;
        }
        for (int z = 0; z < h; z++)
        {
            map[0, z] = true;
            map[w - 1, z] = true;
        }
    }

    #endregion

    #region Connectivity (Flood Fill + Tunnel)

    private void EnsureConnectivity(bool[,] wallMap, int w, int h)
    {
        var regions = FindRegions(wallMap, w, h);
        if (regions.Count <= 1) return;

        regions.Sort((a, b) => b.Count.CompareTo(a.Count));
        var mainRegion = regions[0];

        for (int i = 1; i < regions.Count; i++)
        {
            var (bestA, bestB) = FindClosestPair(mainRegion, regions[i]);
            CarveTunnel(wallMap, bestA, bestB, w, h);
            mainRegion.AddRange(regions[i]);
        }
    }

    private List<List<Vector2Int>> FindRegions(bool[,] wallMap, int w, int h)
    {
        var visited = new bool[w, h];
        var regions = new List<List<Vector2Int>>();

        for (int x = 1; x < w - 1; x++)
        {
            for (int z = 1; z < h - 1; z++)
            {
                if (wallMap[x, z] || visited[x, z]) continue;
                regions.Add(FloodFill(wallMap, visited, x, z, w, h));
            }
        }
        return regions;
    }

    private List<Vector2Int> FloodFill(bool[,] wallMap, bool[,] visited, int startX, int startZ, int w, int h)
    {
        var region = new List<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startZ));
        visited[startX, startZ] = true;

        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();
            region.Add(pos);

            for (int d = 0; d < 4; d++)
            {
                int nx = pos.x + DirX[d];
                int nz = pos.y + DirZ[d];
                if (nx < 1 || nx >= w - 1 || nz < 1 || nz >= h - 1) continue;
                if (visited[nx, nz] || wallMap[nx, nz]) continue;
                visited[nx, nz] = true;
                queue.Enqueue(new Vector2Int(nx, nz));
            }
        }
        return region;
    }

    private (Vector2Int, Vector2Int) FindClosestPair(List<Vector2Int> regionA, List<Vector2Int> regionB)
    {
        Vector2Int bestA = regionA[0];
        Vector2Int bestB = regionB[0];
        float bestDist = float.MaxValue;

        int stepA = Mathf.Max(1, regionA.Count / RegionSampleLimit);
        int stepB = Mathf.Max(1, regionB.Count / RegionSampleLimit);

        for (int a = 0; a < regionA.Count; a += stepA)
        {
            for (int b = 0; b < regionB.Count; b += stepB)
            {
                float dist = (regionA[a] - regionB[b]).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestA = regionA[a];
                    bestB = regionB[b];
                }
            }
        }
        return (bestA, bestB);
    }

    private void CarveTunnel(bool[,] wallMap, Vector2Int from, Vector2Int to, int w, int h)
    {
        int x = from.x;
        int z = from.y;

        while (x != to.x || z != to.y)
        {
            if (x >= 1 && x < w - 1 && z >= 1 && z < h - 1)
                wallMap[x, z] = false;

            if (x < to.x) x++;
            else if (x > to.x) x--;
            else if (z < to.y) z++;
            else if (z > to.y) z--;
        }
    }

    #endregion

    #region Tile & Height Maps

    private ETileType[,] BuildTileMap(DungeonMapConfig dc, System.Random rng)
    {
        int w = dc.Width;
        int h = dc.Height;
        var map = new ETileType[w, h];

        if (dc.TileWeights == null || dc.TileWeights.Length == 0)
        {
            for (int x = 0; x < w; x++)
                for (int z = 0; z < h; z++)
                    map[x, z] = dc.DefaultTileType;
            return map;
        }

        float totalWeight = 0f;
        foreach (var tw in dc.TileWeights)
            totalWeight += tw.Weight;

        float offsetX = (float)rng.NextDouble() * 10000f;
        float offsetZ = (float)rng.NextDouble() * 10000f;

        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < h; z++)
            {
                float noise = Mathf.PerlinNoise(
                    (x + offsetX) * dc.TileNoiseScale,
                    (z + offsetZ) * dc.TileNoiseScale);
                map[x, z] = PickTileByWeight(dc.TileWeights, noise, totalWeight);
            }
        }
        return map;
    }

    private int[,] BuildFloorHeightMap(MapConfig config, System.Random rng)
    {
        int w = config.Width;
        int h = config.Height;
        int extraMax = config.MaxHeight - config.BaseHeight - 1;
        float offsetX = (float)rng.NextDouble() * 10000f;
        float offsetZ = (float)rng.NextDouble() * 10000f;

        var map = new int[w, h];
        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < h; z++)
            {
                float noise = Mathf.PerlinNoise(
                    (x + offsetX) * config.NoiseScale,
                    (z + offsetZ) * config.NoiseScale);
                map[x, z] = config.BaseHeight + Mathf.RoundToInt(noise * extraMax);
            }
        }
        return map;
    }

    private static ETileType PickTileByWeight(TileWeightEntry[] weights, float noise, float totalWeight)
    {
        float scaled = noise * totalWeight;
        float cumulative = 0f;
        foreach (var tw in weights)
        {
            cumulative += tw.Weight;
            if (scaled <= cumulative)
                return tw.TileType;
        }
        return weights[^1].TileType;
    }

    #endregion

    #region Terrain Fill

    private void FillTerrain(TerrainGridData gridData, DungeonMapConfig dc, bool[,] wallMap, ETileType[,] tileMap, int[,] floorHeightMap, System.Random rng)
    {
        int w = dc.Width;
        int h = dc.Height;
        int maxY = dc.MaxHeight;

        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < h; z++)
            {
                if (wallMap[x, z])
                    FillWallColumn(gridData, x, z, maxY, dc.WallTileType);
                else
                    FillOpenColumn(gridData, x, z, floorHeightMap[x, z], maxY, tileMap[x, z], dc.WallTileType, dc, rng);
            }
        }
    }

    private void FillWallColumn(TerrainGridData gridData, int x, int z, int maxY, ETileType wallTile)
    {
        for (int y = 0; y < maxY; y++)
        {
            gridData.SetCell(new Vector3Int(x, y, z), new TerrainCellData(
                ECellType.Dirt, wallTile, 1, isIndestructible: true));
        }
    }

    private void FillOpenColumn(TerrainGridData gridData, int x, int z, int floorHeight, int maxY, ETileType floorTile, ETileType ceilingTile, MapConfig config, System.Random rng)
    {
        for (int y = 0; y < floorHeight; y++)
        {
            gridData.SetCell(new Vector3Int(x, y, z), new TerrainCellData(
                ECellType.Dirt, floorTile, 1, isIndestructible: y == 0));
        }

        gridData.SetCell(new Vector3Int(x, maxY - 1, z), new TerrainCellData(
            ECellType.Dirt, ceilingTile, 1, isIndestructible: true));

        ResourcePlacer.TryPlace(gridData, new Vector3Int(x, floorHeight - 1, z), config.Resources, rng);
    }

    #endregion

    #region Spawn Point

    private static Vector3Int FindOpenSpawn(bool[,] wallMap, TerrainGridData gridData, MapConfig config)
    {
        int w = config.Width;
        int h = config.Height;
        int cx = w / 2;
        int cz = h / 2;

        for (int r = 0; r < Mathf.Max(w, h); r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dz) != r) continue;
                    int x = cx + dx;
                    int z = cz + dz;
                    if (x < 1 || x >= w - 1 || z < 1 || z >= h - 1) continue;
                    if (wallMap[x, z]) continue;

                    for (int y = config.MaxHeight - 2; y >= 0; y--)
                    {
                        if (!gridData.HasCell(new Vector3Int(x, y, z))) continue;
                        var above = new Vector3Int(x, y + 1, z);
                        if (!gridData.HasCell(above))
                            return above;
                    }
                }
            }
        }

        return new Vector3Int(cx, config.BaseHeight, cz);
    }

    #endregion
}