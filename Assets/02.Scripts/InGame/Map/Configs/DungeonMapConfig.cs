using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/Dungeon Config")]
public class DungeonMapConfig : MapConfig
{
    [Header("Dungeon")]
    public string DungeonName = "던전";
    public float TimeLimitSeconds = 180f;
    public int EntryCost = 100;
    public DungeonMaterialRequirement[] EntryRequirements;

    [Header("Multiple Tile Types")]
    public TileWeightEntry[] TileWeights;

    [Header("Cave (Dungeon 2)")]
    public bool IsCave = false;
    [Range(0.40f, 0.55f)]
    public float CaveFillPercent = 0.45f;
    public int CaveSmoothIterations = 5;
    public ETileType WallTileType = ETileType.Dungeon2Stone;

    [Header("Environment")]
    public DungeonEnvironmentProfile EnvironmentProfile;

    public override IMapGenerator CreateGenerator() =>
        IsCave ? new CaveMapGenerator() : new HeightMapGenerator();
}

[Serializable]
public class TileWeightEntry
{
    public ETileType TileType;
    [Range(0f, 1f)]
    public float Weight = 1f;
}

[Serializable]
public class DungeonMaterialRequirement
{
    public ItemDataSO Item;
    public int Amount = 1;
}
