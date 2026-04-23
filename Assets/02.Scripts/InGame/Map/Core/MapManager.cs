using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [SerializeField] private TerrainGridManager _gridManager;

    [Header("Map Configs")]
    [SerializeField] private MapConfig _villageConfig;
    [SerializeField] private DungeonMapConfig[] _dungeonConfigs;

    public TerrainGridManager GridManager => _gridManager;
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

    // player가 null이 아니면 맵 중앙 최상단 위에 자동 배치.
    // 반환값: 스폰 월드 좌표.
    public Vector3 GenerateVillage(Transform player = null)
    {
        int seed = System.Environment.TickCount;
        var result = _villageConfig.CreateGenerator().Generate(_villageConfig, seed);
        _gridManager.LoadFromData(result.GridData);
        _gridManager.SetMaxHeight(_villageConfig.MaxHeight);
        CurrentMap = EMapType.Village;
        PlayCurrentMapBgm();

        if (player != null)
            PlacePlayer(player, result.SpawnPoint);

        return _gridManager.GridToWorld(result.SpawnPoint);
    }

    // 던전 입장 (seed를 공유하면 동일한 맵 생성)
    public void EnterDungeon(int floor, int seed, Transform player = null)
    {
        int configIndex = floor - 1;
        if (configIndex < 0 || configIndex >= _dungeonConfigs.Length)
        {
            Debug.LogError($"No config for dungeon floor {floor}");
            return;
        }

        var config = _dungeonConfigs[configIndex];
        var result = config.CreateGenerator().Generate(config, seed);
        _gridManager.LoadFromData(result.GridData);
        _gridManager.SetMaxHeight(config.MaxHeight);

        CurrentMap = floor switch
        {
            1 => EMapType.Dungeon1,
            2 => EMapType.Dungeon2,
            _ => EMapType.Dungeon3
        };
        PlayCurrentMapBgm();

        if (player != null)
            PlacePlayer(player, result.SpawnPoint);
    }

    public void ExitDungeon()
    {
        CurrentMap = EMapType.Village;
    }

    // 세이브 데이터로 마을 복원 시 호출. MaxHeight도 함께 설정.
    public void ImportVillageSaveData(TerrainSaveData saveData)
    {
        _gridManager.ImportSaveData(saveData);
        _gridManager.SetMaxHeight(_villageConfig.MaxHeight);
        CurrentMap = EMapType.Village;
        PlayCurrentMapBgm();
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

    private void PlayCurrentMapBgm()
    {
        if (SoundManager.Instance == null)
            return;

        switch (CurrentMap)
        {
            case EMapType.Village:
                SoundManager.Instance.CrossfadeBgm(AssetKey.BGM.Village);
                break;
            case EMapType.Dungeon1:
                SoundManager.Instance.CrossfadeBgm(AssetKey.BGM.Dungeon1);
                break;
            case EMapType.Dungeon2:
                SoundManager.Instance.CrossfadeBgm(AssetKey.BGM.Dungeon2);
                break;
        }
    }
}
