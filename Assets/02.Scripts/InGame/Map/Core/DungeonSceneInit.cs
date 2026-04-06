using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

public class DungeonSceneInit : MonoBehaviourPunCallbacks
{
    [SerializeField] private int _floor = 1;
    [SerializeField] private DungeonEnvironmentController _environmentController;

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

        float timeout = Time.time + SeedSyncTimeoutSeconds;
        await UniTask.WaitUntil(() => synced || Time.time > timeout);

        if (!synced)
        {
            Debug.LogWarning("[DungeonSceneInit] Dungeon seed receive timeout");
            return;
        }

        _floor = receivedFloor;

        ApplyObjectPrefabs(receivedFloor);
        MapManager.Instance.EnterDungeon(receivedFloor, receivedSeed);
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
        float progressTimeout = Time.time + TerrainReadyTimeout;

        while (!AllPlayersTerrainReady() && Time.time <= progressTimeout)
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
        RestoreLocalPlayersAfterDungeonLoad();
        RefreshLocalPlayerCameras();
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

        ApplyEnvironment();
        SpawnCliff();
        SceneTransitionData.Clear();
    }

    private void PlaceAllPlayers()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players.Length == 0)
            return;

        Vector3 spawnPos = FindSpawnPosition();

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

    private Vector3 FindSpawnPosition()
    {
        DungeonMapConfig config = MapManager.Instance.GetDungeonConfig(_floor);
        var gridManager = MapManager.Instance.GridManager;
        var gridData = gridManager.GetGridData();

        if (config != null && config.SpawnMode == DungeonSpawnMode.TopCellWithAllowedTile)
        {
            if (TryFindAllowedTileSpawnPosition(gridData, gridManager, config, out Vector3 allowedSpawn))
                return allowedSpawn;

            Debug.LogWarning("[DungeonSceneInit] No valid allowed-tile spawn found. Falling back to center-top spawn.");
        }

        return FindCenterTopSpawnPosition(gridData, gridManager);
    }

    private static Vector3 FindCenterTopSpawnPosition(TerrainGridData gridData, TerrainGridManager gridManager)
    {
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minZ = int.MaxValue;
        int maxZ = int.MinValue;

        foreach (var pos in gridData.Cells.Keys)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.z < minZ) minZ = pos.z;
            if (pos.z > maxZ) maxZ = pos.z;
        }

        int cx = (minX + maxX) / 2;
        int cz = (minZ + maxZ) / 2;

        for (int y = 20; y >= 0; y--)
        {
            if (gridData.HasCell(new Vector3Int(cx, y, cz)))
                return gridManager.GridToWorld(new Vector3Int(cx, y + 1, cz));
        }

        return Vector3.zero;
    }

    private static bool TryFindAllowedTileSpawnPosition(TerrainGridData gridData, TerrainGridManager gridManager, DungeonMapConfig config, out Vector3 spawnPosition)
    {
        if (config.AllowedSpawnTiles == null || config.AllowedSpawnTiles.Length == 0)
        {
            spawnPosition = default;
            return false;
        }

        int centerX = config.Width / 2;
        int centerZ = config.Height / 2;
        bool found = false;
        Vector3Int bestCell = default;
        int bestDistance = int.MaxValue;

        foreach (var kvp in gridData.Cells)
        {
            Vector3Int pos = kvp.Key;
            TerrainCellData cell = kvp.Value;

            if (!cell.IsTop)
                continue;

            if (!IsAllowedSpawnTile(cell.TileType, config.AllowedSpawnTiles))
                continue;

            int distance = Mathf.Abs(pos.x - centerX) + Mathf.Abs(pos.z - centerZ);
            if (!found || distance < bestDistance)
            {
                found = true;
                bestCell = pos;
                bestDistance = distance;
            }
        }

        if (!found)
        {
            spawnPosition = default;
            return false;
        }

        spawnPosition = gridManager.GridToWorld(bestCell + Vector3Int.up);
        return true;
    }

    private static bool IsAllowedSpawnTile(ETileType tileType, ETileType[] allowedTiles)
    {
        foreach (ETileType allowedTile in allowedTiles)
        {
            if (tileType == allowedTile)
                return true;
        }

        return false;
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
}
