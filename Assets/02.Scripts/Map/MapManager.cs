using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [SerializeField] private TerrainGridManager _gridManager;

    [Header("Map Configs")]
    [SerializeField] private MapConfig _villageConfig;
    [SerializeField] private DungeonMapConfig[] _dungeonConfigs;

    public EMapType CurrentMap { get; private set; }
    public bool IsVillage => CurrentMap == EMapType.Village;
    public bool IsDungeon => CurrentMap != EMapType.Village;

    private readonly Dictionary<EMapType, IMapGenerator> _generators = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _generators[EMapType.Village] = new HeightMapGenerator();
        _generators[EMapType.Dungeon1] = new HeightMapGenerator();
        _generators[EMapType.Dungeon2] = new CaveMapGenerator();
    }

    /// Generate village terrain (new game only, then use SaveManager to load).
    /// player가 null이 아니면 맵 중앙 최상단 위에 자동 배치.
    public void GenerateVillage(Transform player = null)
    {
        int seed = System.Environment.TickCount;
        var result = _generators[EMapType.Village].Generate(_villageConfig, seed);
        _gridManager.LoadFromData(result.GridData);
        CurrentMap = EMapType.Village;

        if (player != null)
        {
            Vector3 spawnWorld = _gridManager.GridToWorld(result.SpawnPoint);
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.position = spawnWorld;
            if (cc != null) cc.enabled = true;
        }
    }

    /// Enter dungeon floor (1-based). Generates fresh terrain each time.
    public Vector3Int EnterDungeon(int floor)
    {
        var mapType = floor switch
        {
            1 => EMapType.Dungeon1,
            2 => EMapType.Dungeon2,
            _ => EMapType.Dungeon3
        };

        if (!_generators.ContainsKey(mapType))
        {
            Debug.LogError($"Generator not registered for {mapType}");
            return Vector3Int.zero;
        }

        int configIndex = floor - 1;
        if (configIndex >= _dungeonConfigs.Length)
        {
            Debug.LogError($"No config for dungeon floor {floor}");
            return Vector3Int.zero;
        }

        int seed = System.Environment.TickCount;
        var result = _generators[mapType].Generate(_dungeonConfigs[configIndex], seed);
        _gridManager.LoadFromData(result.GridData);
        CurrentMap = mapType;
        return result.SpawnPoint;
    }

    /// Exit dungeon. Call SaveManager.LoadAsync() after this to restore village.
    public void ExitDungeon()
    {
        CurrentMap = EMapType.Village;
    }

    public DungeonMapConfig GetDungeonConfig(int floor)
    {
        int index = floor - 1;
        if (index < 0 || index >= _dungeonConfigs.Length) return null;
        return _dungeonConfigs[index];
    }
}