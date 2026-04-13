using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using System;

public class DungeonSceneInit : MonoBehaviourPunCallbacks
{
    [SerializeField] private int _floor = 1;
    [SerializeField] private DungeonEnvironmentController _environmentController;
    [SerializeField] private DungeonTimer _dungeonTimer;

    private GameObject _spawnedCliff;

    public static int? FloorOverride { get; set; }

    private const string PropTerrainReady = "tRdy";
    private const float TerrainReadyTimeout = 15f;
    private const float SeedSyncTimeoutSeconds = 10f;

    private void Start()
    {
        if (FloorOverride.HasValue)
        {
            _floor = FloorOverride.Value;
            FloorOverride = null;
        }

        PrepareLocalPlayersForDungeonLoad();

        if (PhotonNetwork.IsConnected)
            InitNetworkDungeon();
        else
            InitLocalDungeon();
    }

    private void InitNetworkDungeon()
    {
        if (SceneTransitionData.SeedReady)
        {
            GenerateFromSyncedSeed();
        }
        else if (PhotonNetwork.IsMasterClient)
        {
            InitMasterDungeonFallback();
        }
        else
        {
            WaitForSeedAndGenerate().Forget();
        }
    }

    private void GenerateFromSyncedSeed()
    {
        int seed = SceneTransitionData.DungeonSeed;
        _floor = SceneTransitionData.DungeonFloor;

        ApplyObjectPrefabs(_floor);
        MapManager.Instance.EnterDungeon(_floor, seed);
        DungeonSpawnHelper.SpawnChests(_floor, seed);
        LoadingProgress.Value = 0.7f;
        ApplyEnvironment();
        SpawnCliff();

        if (PhotonNetwork.IsMasterClient && MapSyncManager.Instance != null)
            MapSyncManager.Instance.BroadcastDungeonSeed(_floor, seed);

        WaitForAllTerrainReady().Forget();
    }

    private void InitMasterDungeonFallback()
    {
        int seed = System.Environment.TickCount;

        ApplyObjectPrefabs(_floor);
        MapManager.Instance.EnterDungeon(_floor, seed);
        DungeonSpawnHelper.SpawnChests(_floor, seed);
        LoadingProgress.Value = 0.7f;
        ApplyEnvironment();
        SpawnCliff();

        if (MapSyncManager.Instance != null)
            MapSyncManager.Instance.BroadcastDungeonSeed(_floor, seed);

        WaitForAllTerrainReady().Forget();
    }

    private async UniTaskVoid WaitForSeedAndGenerate()
    {
        if (MapSyncManager.Instance == null)
            return;

        bool synced = false;
        int receivedFloor = 0;
        int receivedSeed = 0;

        MapSyncManager.Instance.OnDungeonSeedReceived += (floor, seed) =>
        {
            receivedFloor = floor;
            receivedSeed = seed;
            synced = true;
        };

        MapSyncManager.Instance.RequestDungeonSeed();

        float timeout = Time.realtimeSinceStartup + SeedSyncTimeoutSeconds;
        await UniTask.WaitUntil(() => synced || Time.realtimeSinceStartup > timeout);

        if (!synced)
        {
            Debug.LogWarning("[DungeonSceneInit] Dungeon seed receive timeout");
            return;
        }

        _floor = receivedFloor;

        ApplyObjectPrefabs(receivedFloor);
        MapManager.Instance.EnterDungeon(receivedFloor, receivedSeed);
        DungeonSpawnHelper.SpawnChests(receivedFloor, receivedSeed);
        LoadingProgress.Value = 0.7f;
        ApplyEnvironment();
        SpawnCliff();

        WaitForAllTerrainReady().Forget();
    }

    private async UniTaskVoid WaitForAllTerrainReady()
    {
        var props = new Hashtable { { PropTerrainReady, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        int totalCount = Mathf.Max(PhotonNetwork.PlayerList.Length, 1);
        float progressTimeout = Time.realtimeSinceStartup + TerrainReadyTimeout;

        while (!AllPlayersTerrainReady() && Time.realtimeSinceStartup <= progressTimeout)
        {
            int terrainReadyCount = CountTerrainReadyPlayers();
            LoadingProgress.Value = 0.7f + (0.3f * terrainReadyCount / totalCount);
            await UniTask.Yield();
        }

        if (!AllPlayersTerrainReady())
            Debug.LogWarning("[DungeonSceneInit] Terrain ready timeout - proceeding");

        LoadingProgress.Value = 1f;
        LoadingProgress.Complete();

        PlaceAllPlayers();
        await UniTask.Yield();

        SpawnTroublemakers();

        RestoreLocalPlayersAfterDungeonLoad();
        RefreshLocalPlayerCameras();
        StartDungeonTimer();
        ClearSceneTransitionRoomProps();
        SceneTransitionData.Clear();
    }

    private bool AllPlayersTerrainReady()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue(PropTerrainReady, out object val))
            {
                if (val is bool b && b)
                    continue;
            }

            return false;
        }

        return true;
    }

    private int CountTerrainReadyPlayers()
    {
        int terrainReadyCount = 0;

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue(PropTerrainReady, out object val) &&
                val is bool isReady &&
                isReady)
            {
                terrainReadyCount++;
            }
        }

        return terrainReadyCount;
    }

    private void PrepareLocalPlayersForDungeonLoad()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (!player.IsMine)
                continue;

            player.LockAction();
            player.SetVisualsVisible(false);
            player.GetAbility<PlayerCameraAbility>()?.SuspendFollowCamera();
        }
    }

    private void RestoreLocalPlayersAfterDungeonLoad()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (!player.IsMine)
                continue;

            player.SetVisualsVisible(true);
            player.UnlockAction();
        }
    }

    private void InitLocalDungeon()
    {
        int seed;

        if (SceneTransitionData.SeedReady)
        {
            seed = SceneTransitionData.DungeonSeed;
            _floor = SceneTransitionData.DungeonFloor;
        }
        else
        {
            seed = System.Environment.TickCount;
        }

        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        ApplyObjectPrefabs(_floor);

        if (players.Length > 0)
            MapManager.Instance.EnterDungeon(_floor, seed, players[0].transform);
        else
            MapManager.Instance.EnterDungeon(_floor, seed);

        DungeonSpawnHelper.SpawnChests(_floor, seed);
        ApplyEnvironment();
        SpawnCliff();
        StartDungeonTimer();
        SceneTransitionData.Clear();
    }

    private void PlaceAllPlayers()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players.Length == 0)
            return;

        Vector3 spawnPos = DungeonSpawnHelper.FindSpawnPosition(_floor);

        foreach (var player in players)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = false;

            player.transform.position = spawnPos;

            if (cc != null)
                cc.enabled = true;
        }
    }

    private void RefreshLocalPlayerCameras()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (!player.IsMine)
                continue;

            player.GetAbility<PlayerCameraAbility>()?.RebindFollowCamera();
        }
    }

    private void StartDungeonTimer()
    {
        if (_dungeonTimer == null) return;

        DungeonMapConfig config = MapManager.Instance.GetDungeonConfig(_floor);
        if (config == null || config.TimeLimitSeconds <= 0f) return;

        _dungeonTimer.StartTimer(config.TimeLimitSeconds);
    }

    private void ApplyObjectPrefabs(int floor)
    {
        DungeonMapConfig config = MapManager.Instance.GetDungeonConfig(floor);
        if (config == null)
            return;

        MapManager.Instance.GridManager.OverrideObjectPrefabs(config.TreePrefab, config.RockPrefab);
    }

    private void SpawnCliff()
    {
        if (_spawnedCliff != null)
            Destroy(_spawnedCliff);

        DungeonMapConfig config = MapManager.Instance.GetDungeonConfig(_floor);
        if (config == null || config.CliffPrefab == null)
            return;

        _spawnedCliff = Instantiate(config.CliffPrefab);
    }

    private void ApplyEnvironment()
    {
        if (_environmentController == null)
            _environmentController = FindFirstObjectByType<DungeonEnvironmentController>();

        if (_environmentController == null)
            return;

        DungeonMapConfig config = MapManager.Instance.GetDungeonConfig(_floor);
        _environmentController.Apply(config);
    }

    private void ClearSceneTransitionRoomProps()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        var clearRoomProps = new Hashtable
        {
            { SceneTransitionRoomProps.TransitionType, null },
            { SceneTransitionRoomProps.DungeonSeed, null },
            { SceneTransitionRoomProps.DungeonFloor, null }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(clearRoomProps);
    }

    private void SpawnTroublemakers()
    {
        TroublemakerSpawner spawner = FindFirstObjectByType<TroublemakerSpawner>();
        if (spawner == null || PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;

        int seed = SceneTransitionData.SeedReady ? SceneTransitionData.DungeonSeed : Environment.TickCount;
        spawner.SpawnAll(_floor, seed);
    }
}
