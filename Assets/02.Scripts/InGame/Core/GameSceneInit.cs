using System;
using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

public class GameSceneInit : MonoBehaviour
{
    public static bool ReturningFromDungeon{ get; set; }
    public static event Action OnCompleteInitialize;

    [SerializeField] private string _playerPrefabName = "Player";
    [SerializeField] private MapManager _mapManager;
    [SerializeField] private MapNavMeshController _mapNavMeshController;
    private const float MapSyncTimeoutSeconds = 10f;
    private const int MaxSpawnCheckHeight = 20;
    private const string PropTerrainReady = "tRdy";
    private const float TerrainReadyTimeout = 15f;

    private void Start()
    {
        if (ReturningFromDungeon)
            FreezeExistingPlayers();

        if (PhotonNetwork.IsConnected)
            InitNetworkGame();
        else
            InitLocalGame();
    }

    private void InitNetworkGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (RoomManager.Instance.IsFirstVisit && !ReturningFromDungeon)
            {
                Vector3 spawnPos = _mapManager.GenerateVillage();
                _mapNavMeshController.BuildInitialNavMesh();
                SpawnPlayer(spawnPos);
                CacheVillageData();

                var props = new Hashtable { { PropTerrainReady, true } };
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                LoadingProgress.Value = 1f;
                LoadingProgress.Complete();

                RoomManager.Instance.OpenRoom();
                OnCompleteInitialize?.Invoke();
            }
            else
            {
                LoadAndSpawnMaster().Forget();
            }
        }
        else
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            WaitForMapAndSpawn().Forget();
        }
    }

    private async UniTaskVoid WaitForMapAndSpawn()
    {
        if (PhotonNetwork.IsConnected)
        {
            var props = new Hashtable { { PropTerrainReady, false } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        // 던전 복귀 시 캐시에서 마을 복원
        if (ReturningFromDungeon && VillageCache.HasCache)
        {
            _mapManager.ImportVillageSaveData(VillageCache.Terrain);

            if (VillageCache.Buildings != null && BuildingManager.Instance != null)
                await BuildingManager.Instance.ImportBuildings(VillageCache.Buildings);

            VillageCache.RestorePlayerPositions();
            LoadingProgress.Value = 0.6f;
            ReturningFromDungeon = false;

            await WaitForAllTerrainReady();
            return;
        }

        // 일반 입장: 마스터로부터 맵 동기화
        if (!PhotonNetwork.InRoom) return;
        if (MapSyncManager.Instance == null) return;

        bool synced = false;
        MapSyncManager.Instance.OnMapSynced += () => synced = true;
        MapSyncManager.Instance.RequestMapFromMaster();

        float timeout = Time.realtimeSinceStartup + MapSyncTimeoutSeconds;
        await UniTask.WaitUntil(() => synced || Time.realtimeSinceStartup > timeout);

        if (!synced)
        {
            Debug.LogError("[GameSceneInit] Map sync failed");
            LoadingProgress.Value = 1f;
            LoadingProgress.Complete();
            UnfreezeExistingPlayers();
            return;
        }

        LoadingProgress.Value = 0.6f;

        var existing = FindAnyObjectByType<PlayerController>();
        if (existing != null && existing.IsMine)
        {
            // 기존 플레이어 사용 — 스폰 건너뜀
        }
        else
        {
            var pos = FindSpawnPosition();
            SpawnPlayer(pos);
        }

        CacheVillageData();
        await WaitForAllTerrainReady();
        OnCompleteInitialize?.Invoke();
    }

    private Vector3 FindSpawnPosition()
    {
        var gridManager = _mapManager.GridManager;
        var gridData = gridManager.GetGridData();

        // 맵 중앙 좌표 계산
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;
        foreach (var pos in gridData.Cells.Keys)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.z < minZ) minZ = pos.z;
            if (pos.z > maxZ) maxZ = pos.z;
        }

        int cx = (minX + maxX) / 2;
        int cz = (minZ + maxZ) / 2;

        for (int y = MaxSpawnCheckHeight; y >= 0; y--)
        {
            if (gridData.HasCell(new Vector3Int(cx, y, cz)))
                return gridManager.GridToWorld(new Vector3Int(cx, y + 1, cz));
        }

        return Vector3.zero;
    }

    private void SpawnPlayer(Vector3 spawnPos)
    {
        var playerObj = PhotonNetwork.Instantiate(_playerPrefabName, spawnPos, Quaternion.identity);
        var pc = playerObj.GetComponent<PlayerController>();

        if (CustomizeData.Instance != null)
            pc.GetAbility<PlayerCustomizeAbility>()?.Initialize(CustomizeData.Instance.Data);
    }

    private void InitLocalGame()
    {
        if (ReturningFromDungeon)
        {
            LoadLocalVillage().Forget();
            return;
        }

        var existing = FindAnyObjectByType<PlayerController>();

        _mapManager.GenerateVillage(existing != null ? existing.transform : null);
        _mapNavMeshController.BuildInitialNavMesh();
        CacheVillageData();

        if (existing != null && CustomizeData.Instance != null)
            existing.GetAbility<PlayerCustomizeAbility>()?.Initialize(CustomizeData.Instance.Data);

        OnCompleteInitialize?.Invoke();
    }

    private async UniTaskVoid LoadLocalVillage()
    {
        if (VillageCache.HasCache)
        {
            _mapManager.ImportVillageSaveData(VillageCache.Terrain);
            _mapNavMeshController.BuildInitialNavMesh();

            if (VillageCache.Buildings != null && BuildingManager.Instance != null)
                await BuildingManager.Instance.ImportBuildings(VillageCache.Buildings);

            VillageCache.RestorePlayerPositions();
        }
        else
        {
            int slot = RoomManager.Instance != null ? RoomManager.Instance.SelectedSlot : 0;
            await SaveManager.Instance.LoadAsync(slot);
            _mapNavMeshController.BuildInitialNavMesh();
            CacheVillageData();
        }

        if (BuildingManager.Instance != null)
            BuildingManager.Instance.SpawnBuildingNpcs();

        var existing = FindAnyObjectByType<PlayerController>();
        if (existing != null && SaveManager.Instance != null)
            SaveManager.Instance.RegisterPlayer(existing.PlayerId, existing);

        ReturningFromDungeon = false;
        UnfreezeExistingPlayers();
        SceneTransitionData.Clear();
        OnCompleteInitialize?.Invoke();
    }

    private async UniTaskVoid LoadAndSpawnMaster()
    {
        int slot = RoomManager.Instance.SelectedSlot;
        bool returning = ReturningFromDungeon;

        if (returning && VillageCache.HasCache)
        {
            // 캐시에서 마을 복원 (파일 I/O 없이)
            _mapManager.ImportVillageSaveData(VillageCache.Terrain);
            
            if (VillageCache.Buildings != null && BuildingManager.Instance != null)
                await BuildingManager.Instance.ImportBuildings(VillageCache.Buildings);
            
            _mapNavMeshController.BuildInitialNavMesh();

            if (VillageCache.Buildings != null && BuildingManager.Instance != null)
                BuildingManager.Instance.SpawnBuildingNpcs();
            
            VillageCache.RestorePlayerPositions();
            RestoreExistingPlayers();
            ReturningFromDungeon = false;
        }
        else
        {
            // 세이브 파일에서 로드
            if (MapSyncManager.Instance != null)
                MapSyncManager.Instance.HoldRequests();

            await SaveManager.Instance.LoadAsync(slot);
            _mapNavMeshController.BuildInitialNavMesh();

            if (BuildingManager.Instance != null)
                BuildingManager.Instance.SpawnBuildingNpcs();
            
            if (ReturningFromDungeon)
            {
                RestoreExistingPlayers();
                ReturningFromDungeon = false;
            }
            else
            {
                SpawnPlayer(Vector3.zero);
            }

            await UniTask.Yield();

            if (MapSyncManager.Instance != null)
                MapSyncManager.Instance.BroadcastMap();

            CacheVillageData();
        }

        LoadingProgress.Value = 0.6f;
        RoomManager.Instance.OpenRoom();
        await WaitForAllTerrainReady();
        OnCompleteInitialize?.Invoke();
    }

    private async UniTask WaitForAllTerrainReady()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneTransitionData.Clear();
            return;
        }

        LockAllLocalPlayers();
        LoadingProgress.Value = 0.7f;

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
            Debug.LogWarning("[GameSceneInit] Terrain ready timeout - proceeding");

        LoadingProgress.Value = 1f;
        LoadingProgress.Complete();

        UnfreezeExistingPlayers();
        ClearSceneTransitionRoomProps();
        SceneTransitionData.Clear();
    }

    private bool AllPlayersTerrainReady()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.TryGetValue(PropTerrainReady, out object val))
            {
                if (val is bool b && b) continue;
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

    private void FreezeExistingPlayers()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (!p.IsMine) continue;

            var cc = p.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            p.LockAction();
            p.SetVisualsVisible(false);
            p.GetAbility<PlayerCameraAbility>()?.SuspendFollowCamera();
        }
    }

    private void UnfreezeExistingPlayers()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (!p.IsMine) continue;

            var cc = p.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;

            p.SetVisualsVisible(true);
            p.UnlockAction();
            p.GetAbility<PlayerCameraAbility>()?.RebindFollowCamera();
        }
    }

    private void LockAllLocalPlayers()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.IsMine)
                p.LockAction();
        }
    }

    private void RestoreExistingPlayers()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (SaveManager.Instance != null)
                SaveManager.Instance.RegisterPlayer(player.PlayerId, player);
        }
    }

    private void CacheVillageData()
    {
        VillageCache.Capture(_mapManager.GridManager);
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
