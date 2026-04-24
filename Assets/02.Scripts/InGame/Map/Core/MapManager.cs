using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [SerializeField] private TerrainGridManager _gridManager;

    [Header("Map Configs")]
    [SerializeField] private MapConfig _villageConfig;
    [SerializeField] private DungeonMapConfig[] _dungeonConfigs;

    [Header("Map Audio")]
    [SerializeField, Min(0f)] private float _bgmFadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float _bgmFadeInDuration = 1f;
    [SerializeField, Range(0f, 1f)] private float _lavaDungeonEffectVolume = 0.7f;
    [SerializeField, Min(0f)] private float _lavaDungeonEffectFadeInDuration = 0.6f;
    [SerializeField, Min(0f)] private float _lavaDungeonEffectFadeOutDuration = 1.2f;

    private const string LavaDungeonEffectLoopKey = "Dungeon3_LavaDungeonEffect";

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
        PlayCurrentMapAudio();

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
        PlayCurrentMapAudio();

        if (player != null)
            PlacePlayer(player, result.SpawnPoint);
    }

    public void ExitDungeon()
    {
        CurrentMap = EMapType.Village;
        PlayCurrentMapAudio();
    }

    // 세이브 데이터로 마을 복원 시 호출. MaxHeight도 함께 설정.
    public void ImportVillageSaveData(TerrainSaveData saveData)
    {
        _gridManager.ImportSaveData(saveData);
        _gridManager.SetMaxHeight(_villageConfig.MaxHeight);
        CurrentMap = EMapType.Village;
        PlayCurrentMapAudio();
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

    private void PlayCurrentMapAudio()
    {
        if (SoundManager.Instance == null)
            return;

        StopLavaDungeonEffectIfNeeded();
        BgmTransitionConfig bgmConfig = new BgmTransitionConfig(_bgmFadeOutDuration, _bgmFadeInDuration);

        switch (CurrentMap)
        {
            case EMapType.Village:
                SoundManager.Instance.CrossfadeBgm(AssetKey.BGM.Village, bgmConfig);
                break;
            case EMapType.Dungeon1:
                SoundManager.Instance.CrossfadeBgm(AssetKey.BGM.Dungeon1, bgmConfig);
                break;
            case EMapType.Dungeon2:
                SoundManager.Instance.CrossfadeBgm(AssetKey.BGM.Dungeon2, bgmConfig);
                break;
            case EMapType.Dungeon3:
                SoundManager.Instance.CrossfadeBgm(AssetKey.BGM.LavaDungeon, bgmConfig);
                PlayLavaDungeonEffect();
                break;
        }
    }

    private void PlayLavaDungeonEffect()
    {
        SoundManager.Instance.PlayLoopingSfx(
            LavaDungeonEffectLoopKey,
            new SfxPlayRequest(
                clipKey: AssetKey.SFX.LavaDungeonEffect,
                spatialMode: ESpatialMode.Flat2D,
                volume: _lavaDungeonEffectVolume),
            _lavaDungeonEffectFadeInDuration);
    }

    private void StopLavaDungeonEffectIfNeeded()
    {
        if (CurrentMap == EMapType.Dungeon3)
            return;

        SoundManager.Instance.StopLoopingSfx(LavaDungeonEffectLoopKey, _lavaDungeonEffectFadeOutDuration);
    }
}
