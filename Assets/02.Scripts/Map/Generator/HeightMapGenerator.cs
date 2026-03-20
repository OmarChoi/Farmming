using UnityEngine;

public class HeightMapGenerator : IMapGenerator
{
    public MapGenerationResult Generate(MapConfig config, int seed)
    {
        var gridData = new TerrainGridData();
        var rng = new System.Random(seed);
        float offsetX = (float)rng.NextDouble() * 10000f;
        float offsetZ = (float)rng.NextDouble() * 10000f;

        int w = config.Width;
        int h = config.Height;
        int extraMax = config.MaxHeight - config.BaseHeight;

        ETileType[] tileMap = BuildTileMap(config, rng);

        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < h; z++)
            {
                float noise = Mathf.PerlinNoise(
                    (x + offsetX) * config.NoiseScale,
                    (z + offsetZ) * config.NoiseScale);
                int extraHeight = Mathf.RoundToInt(noise * extraMax);
                int totalHeight = config.BaseHeight + extraHeight;

                ETileType tile = tileMap != null
                    ? tileMap[x * h + z]
                    : config.DefaultTileType;

                for (int y = 0; y < totalHeight; y++)
                {
                    bool isBottom = (y == 0);
                    gridData.SetCell(new Vector3Int(x, y, z), new TerrainCellData(
                        ECellType.Dirt,
                        tile,
                        dirtLevel: 1,
                        isIndestructible: isBottom
                    ));
                }

                int topY = totalHeight - 1;
                ResourcePlacer.TryPlace(gridData, new Vector3Int(x, topY, z), config.Resources, rng);
            }
        }

        return new MapGenerationResult
        {
            GridData = gridData,
            SpawnPoint = FindSpawnPoint(gridData, w, h)
        };
    }

    private ETileType[] BuildTileMap(MapConfig config, System.Random rng)
    {
        if (config is not DungeonMapConfig dc) return null;
        if (dc.TileWeights == null || dc.TileWeights.Length <= 1) return null;

        int w = config.Width;
        int h = config.Height;
        var map = new ETileType[w * h];

        float offsetX = (float)rng.NextDouble() * 10000f;
        float offsetZ = (float)rng.NextDouble() * 10000f;

        float totalWeight = 0f;
        foreach (var tw in dc.TileWeights)
            totalWeight += tw.Weight;

        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < h; z++)
            {
                float noise = Mathf.PerlinNoise(
                    (x + offsetX) * 0.08f,
                    (z + offsetZ) * 0.08f);
                map[x * h + z] = PickTileByWeight(dc.TileWeights, noise, totalWeight);
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
        return weights[weights.Length - 1].TileType;
    }

    private static Vector3Int FindSpawnPoint(TerrainGridData gridData, int width, int height)
    {
        int cx = width / 2;
        int cz = height / 2;

        for (int y = 7; y >= 0; y--)
        {
            var pos = new Vector3Int(cx, y, cz);
            if (gridData.HasCell(pos))
                return new Vector3Int(cx, y + 1, cz);
        }

        return new Vector3Int(cx, 4, cz);
    }
}