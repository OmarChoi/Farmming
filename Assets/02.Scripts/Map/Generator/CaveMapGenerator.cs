using System.Collections.Generic;
using UnityEngine;

public class CaveMapGenerator : IMapGenerator
{
    public MapGenerationResult Generate(MapConfig config, int seed)
    {
        var dc = (DungeonMapConfig)config;
        var gridData = new TerrainGridData();
        var rng = new System.Random(seed);

        int w = config.Width;
        int h = config.Height;
        int maxY = config.MaxHeight;

        // 1. Cellular Automata -> pillar/wall map
        bool[,] wallMap = GenerateWalls(w, h, dc.CaveFillPercent, dc.CaveSmoothIterations, seed);

        // 2. Seal edges
        SealEdges(wallMap, w, h);

        // 3. Ensure connectivity (entrance reachable)
        EnsureConnectivity(wallMap, w, h, rng);

        // 4. Build tile map for floor variety
        ETileType[] tileMap = BuildCaveTileMap(dc, w, h, rng);

        // 5. Perlin Noise offsets for floor height variation
        float noiseOx = (float)rng.NextDouble() * 10000f;
        float noiseOz = (float)rng.NextDouble() * 10000f;

        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < h; z++)
            {
                if (wallMap[x, z])
                {
                    // Wall/pillar: fill floor to ceiling, all indestructible
                    for (int y = 0; y < maxY; y++)
                    {
                        gridData.SetCell(new Vector3Int(x, y, z), new TerrainCellData(
                            ECellType.Dirt, dc.WallTileType, 1,
                            isIndestructible: true));
                    }
                }
                else
                {
                    ETileType floorTile = tileMap[x * h + z];

                    // Floor height: base + Perlin variation (leave room for ceiling)
                    float noise = Mathf.PerlinNoise(
                        (x + noiseOx) * config.NoiseScale,
                        (z + noiseOz) * config.NoiseScale);
                    int extraMax = maxY - config.BaseHeight - 1; // -1 for ceiling
                    int extraHeight = Mathf.RoundToInt(noise * extraMax);
                    int floorHeight = config.BaseHeight + extraHeight;

                    // Floor blocks
                    for (int y = 0; y < floorHeight; y++)
                    {
                        gridData.SetCell(new Vector3Int(x, y, z), new TerrainCellData(
                            ECellType.Dirt, floorTile, 1,
                            isIndestructible: (y == 0)));
                    }

                    // Ceiling block (top layer, indestructible)
                    gridData.SetCell(new Vector3Int(x, maxY - 1, z), new TerrainCellData(
                        ECellType.Dirt, dc.WallTileType, 1,
                        isIndestructible: true));

                    // Resource on top of floor
                    int topY = floorHeight - 1;
                    ResourcePlacer.TryPlace(gridData, new Vector3Int(x, topY, z), config.Resources, rng);
                }
            }
        }

        Vector3Int spawn = FindOpenSpawn(wallMap, gridData, w, h);
        return new MapGenerationResult
        {
            GridData = gridData,
            SpawnPoint = spawn
        };
    }

    private bool[,] GenerateWalls(int w, int h, float fillPercent, int smoothIterations, int seed)
    {
        var rng = new System.Random(seed);
        var map = new bool[w, h];

        // Random fill
        for (int x = 0; x < w; x++)
            for (int z = 0; z < h; z++)
                map[x, z] = rng.NextDouble() < fillPercent;

        // Smooth
        for (int i = 0; i < smoothIterations; i++)
        {
            var next = new bool[w, h];
            for (int x = 0; x < w; x++)
            {
                for (int z = 0; z < h; z++)
                {
                    int wallCount = CountWallNeighbors(map, x, z, w, h);
                    next[x, z] = wallCount > 4;
                }
            }
            map = next;
        }

        return map;
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

    private void EnsureConnectivity(bool[,] wallMap, int w, int h, System.Random rng)
    {
        // Find all open regions via Flood Fill
        var visited = new bool[w, h];
        var regions = new List<List<Vector2Int>>();

        for (int x = 1; x < w - 1; x++)
        {
            for (int z = 1; z < h - 1; z++)
            {
                if (wallMap[x, z] || visited[x, z]) continue;

                var region = new List<Vector2Int>();
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(new Vector2Int(x, z));
                visited[x, z] = true;

                while (queue.Count > 0)
                {
                    var pos = queue.Dequeue();
                    region.Add(pos);

                    int[] dx = { 1, -1, 0, 0 };
                    int[] dz = { 0, 0, 1, -1 };
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = pos.x + dx[d];
                        int nz = pos.y + dz[d];
                        if (nx < 1 || nx >= w - 1 || nz < 1 || nz >= h - 1) continue;
                        if (visited[nx, nz] || wallMap[nx, nz]) continue;
                        visited[nx, nz] = true;
                        queue.Enqueue(new Vector2Int(nx, nz));
                    }
                }

                regions.Add(region);
            }
        }

        if (regions.Count <= 1) return;

        // Sort by size descending, keep largest as main
        regions.Sort((a, b) => b.Count.CompareTo(a.Count));
        var mainRegion = regions[0];

        // Connect smaller regions to main by carving tunnels
        for (int i = 1; i < regions.Count; i++)
        {
            var smallRegion = regions[i];
            // Find closest pair of cells between regions
            Vector2Int bestA = mainRegion[0];
            Vector2Int bestB = smallRegion[0];
            float bestDist = float.MaxValue;

            // Sample to avoid O(n^2) for large regions
            int stepA = Mathf.Max(1, mainRegion.Count / 50);
            int stepB = Mathf.Max(1, smallRegion.Count / 50);

            for (int a = 0; a < mainRegion.Count; a += stepA)
            {
                for (int b = 0; b < smallRegion.Count; b += stepB)
                {
                    float dist = (mainRegion[a] - smallRegion[b]).sqrMagnitude;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestA = mainRegion[a];
                        bestB = smallRegion[b];
                    }
                }
            }

            // Carve tunnel between bestA and bestB
            CarveTunnel(wallMap, bestA, bestB, w, h);

            // Merge into main region
            mainRegion.AddRange(smallRegion);
        }
    }

    private void CarveTunnel(bool[,] wallMap, Vector2Int from, Vector2Int to, int w, int h)
    {
        int x = from.x;
        int z = from.y;

        while (x != to.x || z != to.y)
        {
            // Carve 1-wide path
            if (x >= 1 && x < w - 1 && z >= 1 && z < h - 1)
                wallMap[x, z] = false;

            if (x < to.x) x++;
            else if (x > to.x) x--;
            else if (z < to.y) z++;
            else if (z > to.y) z--;
        }
    }

    private ETileType[] BuildCaveTileMap(DungeonMapConfig dc, int w, int h, System.Random rng)
    {
        var map = new ETileType[w * h];

        if (dc.TileWeights == null || dc.TileWeights.Length == 0)
        {
            for (int i = 0; i < map.Length; i++)
                map[i] = dc.DefaultTileType;
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
                    (x + offsetX) * 0.08f,
                    (z + offsetZ) * 0.08f);
                float scaled = noise * totalWeight;
                float cumulative = 0f;
                ETileType picked = dc.TileWeights[0].TileType;
                foreach (var tw in dc.TileWeights)
                {
                    cumulative += tw.Weight;
                    if (scaled <= cumulative)
                    {
                        picked = tw.TileType;
                        break;
                    }
                }
                map[x * h + z] = picked;
            }
        }

        return map;
    }

    private Vector3Int FindOpenSpawn(bool[,] wallMap, TerrainGridData gridData, int w, int h)
    {
        // Find an open cell near center
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

                    // Find top floor cell
                    for (int y = 6; y >= 0; y--)
                    {
                        var pos = new Vector3Int(x, y, z);
                        if (gridData.HasCell(pos))
                        {
                            var above = new Vector3Int(x, y + 1, z);
                            if (!gridData.HasCell(above))
                                return above;
                        }
                    }
                }
            }
        }

        return new Vector3Int(cx, 4, cz);
    }
}