public interface IMapGenerator
{
    MapGenerationResult Generate(MapConfig config, int seed);
}