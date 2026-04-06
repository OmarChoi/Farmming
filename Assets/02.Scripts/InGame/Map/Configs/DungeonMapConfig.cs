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
    public GameObject CliffPrefab;

    [Header("Object Prefab Overrides")]
    public GameObject TreePrefab;
    public GameObject RockPrefab;

    [Header("Spawn")]
    public DungeonSpawnMode SpawnMode = DungeonSpawnMode.CenterTop;
    public ETileType[] AllowedSpawnTiles;

    [Header("Multiple Tile Types")]
    public TileWeightEntry[] TileWeights;

    [Header("Chest")]
    public GameObject ChestPrefab;
    public int ChestCount = 3;
    public ChestLootTable[] ChestLootTables;

    [Header("Environment")]
    public DungeonEnvironmentProfile EnvironmentProfile;

    public override IMapGenerator CreateGenerator() => new HeightMapGenerator();
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

public enum DungeonSpawnMode
{
    CenterTop,
    TopCellWithAllowedTile,
}