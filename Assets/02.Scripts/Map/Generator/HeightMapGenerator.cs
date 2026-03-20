using UnityEngine;

public class HeightMapGenerator : IMapGenerator
{
    public MapGenerationResult Generate(MapConfig config, int seed)
    {
        var gridData = new TerrainGridData();
        var rng = new System.Random(seed);

        int[,] heightMap = GenerateHeightMap(config, rng);
        ETileType[,] tileMap = GenerateTileMap(config, rng);

        FillTerrain(gridData, config, heightMap, tileMap, rng);

        return new MapGenerationResult
        {
            GridData = gridData,
            SpawnPoint = FindSpawnPoint(gridData, config)
        };
    }

    private int[,] GenerateHeightMap(MapConfig config, System.Random rng)
    {
        int w = config.Width;
        int h = config.Height;
        int extraMax = config.MaxHeight - config.BaseHeight;
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

    private ETileType[,] GenerateTileMap(MapConfig config, System.Random rng)
    {
        int w = config.Width;
        int h = config.Height;
        var map = new ETileType[w, h];

        if (config is not DungeonMapConfig dc || dc.TileWeights == null || dc.TileWeights.Length <= 1)
        {
            for (int x = 0; x < w; x++)
                for (int z = 0; z < h; z++)
                    map[x, z] = config.DefaultTileType;
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
                    (x + offsetX) * config.TileNoiseScale,
                    (z + offsetZ) * config.TileNoiseScale);
                map[x, z] = PickTileByWeight(dc.TileWeights, noise, totalWeight);
            }
        }
        return map;
    }

    private void FillTerrain(TerrainGridData gridData, MapConfig config, int[,] heightMap, ETileType[,] tileMap, System.Random rng)
    {
        int w = config.Width;
        int h = config.Height;

        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < h; z++)
            {
                int totalHeight = heightMap[x, z];
                ETileType tile = tileMap[x, z];

                for (int y = 0; y < totalHeight; y++)
                {
                    gridData.SetCell(new Vector3Int(x, y, z), new TerrainCellData(
                        ECellType.Dirt,
                        tile,
                        dirtLevel: 1,
                        isIndestructible: y == 0,
                        isTop: y == totalHeight - 1
                    ));
                }

                ResourcePlacer.TryPlace(gridData, new Vector3Int(x, totalHeight - 1, z), config.Resources, rng);
            }
        }
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

    private static Vector3Int FindSpawnPoint(TerrainGridData gridData, MapConfig config)
    {
        int cx = config.Width / 2;
        int cz = config.Height / 2;

        for (int y = config.MaxHeight - 1; y >= 0; y--)
        {
            if (gridData.HasCell(new Vector3Int(cx, y, cz)))
                return new Vector3Int(cx, y + 1, cz);
        }

        return new Vector3Int(cx, config.BaseHeight, cz);
    }
}