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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// Generate village terrain (new game only, then use SaveManager to load).
    /// player가 null이 아니면 맵 중앙 최상단 위에 자동 배치.
    /// 반환값: 스폰 월드 좌표.
    public Vector3 GenerateVillage(Transform player = null)
    {
        int seed = System.Environment.TickCount;
        var result = _villageConfig.CreateGenerator().Generate(_villageConfig, seed);
        _gridManager.LoadFromData(result.GridData);
        CurrentMap = EMapType.Village;

        if (player != null)
            PlacePlayer(player, result.SpawnPoint);

        return _gridManager.GridToWorld(result.SpawnPoint);
    }

    /// Enter dungeon floor (1-based). Generates fresh terrain each time.
    public void EnterDungeon(int floor, Transform player = null)
    {
        int configIndex = floor - 1;
        if (configIndex < 0 || configIndex >= _dungeonConfigs.Length)
        {
            Debug.LogError($"No config for dungeon floor {floor}");
            return;
        }

        var config = _dungeonConfigs[configIndex];
        int seed = System.Environment.TickCount;
        var result = config.CreateGenerator().Generate(config, seed);
        _gridManager.LoadFromData(result.GridData);

        CurrentMap = floor switch
        {
            1 => EMapType.Dungeon1,
            2 => EMapType.Dungeon2,
            _ => EMapType.Dungeon3
        };

        if (player != null)
            PlacePlayer(player, result.SpawnPoint);
    }

    /// Exit dungeon. Call SaveManager.LoadAsync() after this to restore village.
    public void ExitDungeon()
    {
        CurrentMap = EMapType.Village;
    }

    private void PlacePlayer(Transform player, Vector3Int spawnPoint)
    {
        Vector3 spawnWorld = _gridManager.GridToWorld(spawnPoint);
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.position = spawnWorld;
        if (cc != null) cc.enabled = true;
    }

    public DungeonMapConfig GetDungeonConfig(int floor)
    {
        int index = floor - 1;
        if (index < 0 || index >= _dungeonConfigs.Length) return null;
        return _dungeonConfigs[index];
    }
}