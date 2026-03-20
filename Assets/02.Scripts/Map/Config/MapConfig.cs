using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/Map Config")]
public class MapConfig : ScriptableObject
{
    [Header("Map Size")]
    public int Width = 75;
    public int Height = 75;
    public int BaseHeight = 4;
    public int MaxHeight = 8;

    [Header("Hill Generation (Perlin Noise)")]
    [Range(0.01f, 0.1f)]
    public float NoiseScale = 0.05f;

    [Header("Default Tile")]
    public ETileType DefaultTileType = ETileType.VillageDirt;

    [Header("Resource Spawning")]
    public ResourceEntry[] Resources;
}

[Serializable]
public class ResourceEntry
{
    public EGridObjectType Type;
    public int Level = 1;
    [Range(0f, 1f)]
    public float SpawnChance = 0.1f;
}