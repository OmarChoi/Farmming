using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

public class GameSceneInit : MonoBehaviour
{
    public static bool ReturningFromDungeon{ get; set; }

    [SerializeField] private string _playerPrefabName = "Player";
    [SerializeField] private MapManager _mapManager;
    [SerializeField] private MapNavMeshController _mapNavMeshController;
    private const float MapSyncTimeoutSeconds = 10f;
    private const int MaxSpawnCheckHeight = 20;
    private const string PropTerrainReady = "tRdy";
    private const float TerrainReadyTimeout = 15f;

    private void Start()
    {
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
                Debug.Log("______________________________처음");
                Vector3 spawnPos = _mapManager.GenerateVillage();
                _mapNavMeshController.BuildInitialNavMesh();
                SpawnPlayer(spawnPos);
                RoomManager.Instance.OpenRoom();
            }
            else
            {
                Debug.Log("______________________________아님");
                LoadAndSpawnMaster().Forget();
            }
        }
        else
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            // 클라이언트: 맵 동기화 완료 후 스폰
            WaitForMapAndSpawn().Forget();
        }
    }

    private async UniTaskVoid WaitForMapAndSpawn()
    {
        if (!PhotonNetwork.InRoom) return;
        if (MapSyncManager.Instance == null) return;

        bool synced = false;
        MapSyncManager.Instance.OnMapSynced += () => synced = true;
        MapSyncManager.Instance.RequestMapFromMaster();

        float timeout = Time.time + MapSyncTimeoutSeconds;
        await UniTask.WaitUntil(() => synced || Time.time > timeout);

        if (!synced) return;
        LoadingProgress.Value = 0.6f;

        // 던전 복귀 시 기존 DontDestroyOnLoad 플레이어가 있으면 중복 스폰 방지
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

        // 지형 준비 완료 → 대기 게이트
        await WaitForAllTerrainReady();
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
            ReturningFromDungeon = false;
            LoadLocalVillage().Forget();
            return;
        }

        var existing = FindAnyObjectByType<PlayerController>();

        Vector3 spawnPos = _mapManager.GenerateVillage(existing != null ? existing.transform : null);
        _mapNavMeshController.BuildInitialNavMesh();

        if (existing != null && CustomizeData.Instance != null)
            existing.GetAbility<PlayerCustomizeAbility>()?.Initialize(CustomizeData.Instance.Data);
    }

    private async UniTaskVoid LoadLocalVillage()
    {
        int slot = RoomManager.Instance != null ? RoomManager.Instance.SelectedSlot : 0;
        await SaveManager.Instance.LoadAsync(slot);

        _mapNavMeshController.BuildInitialNavMesh();

        if (BuildingManager.Instance != null)
            BuildingManager.Instance.SpawnBuildingNpcs();

        var existing = FindAnyObjectByType<PlayerController>();
        if (existing != null)
        {
            string playerId = existing.PlayerId;
            SaveManager.Instance.RegisterPlayer(playerId, existing);
        }

        SceneTransitionData.Clear();
    }

    private async UniTaskVoid LoadAndSpawnMaster()
    {
        int slot = RoomManager.Instance.SelectedSlot;

        // 클라이언트가 로드 완료 전에 맵을 요청하는 것을 방지
        if (MapSyncManager.Instance != null)
            MapSyncManager.Instance.HoldRequests();

        // 1. 맵·지형 데이터 로드 → _loadedData 보관
        await SaveManager.Instance.LoadAsync(slot);

        // 2. 맵 로드 후 NavMesh 빌드
        _mapNavMeshController.BuildInitialNavMesh();

        await SaveManager.Instance.LoadBuildingAsync();

        if (ReturningFromDungeon)
        {
            // 기존 DontDestroyOnLoad 플레이어를 재등록 → 저장된 위치 복원
            RestoreExistingPlayers();
            ReturningFromDungeon = false;
        }
        else
        {
            SpawnPlayer(Vector3.zero);
        }

        // 한 프레임 대기 → Start() 실행 보장
        await UniTask.Yield();

        // 맵 로드 완료 → 클라이언트에 전송
        if (MapSyncManager.Instance != null)
            MapSyncManager.Instance.BroadcastMap();

        LoadingProgress.Value = 0.6f;

        // 클라이언트 입장 허용
        RoomManager.Instance.OpenRoom();

        // 지형 준비 완료 → 대기 게이트
        await WaitForAllTerrainReady();
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
        float progressTimeout = Time.time + TerrainReadyTimeout;

        while (!AllPlayersTerrainReady() && Time.time <= progressTimeout)
        {
            int terrainReadyCount = CountTerrainReadyPlayers();
            LoadingProgress.Value = 0.7f + (0.3f * terrainReadyCount / totalCount);
            await UniTask.Yield();
        }

        float timeout = Time.time + TerrainReadyTimeout;
        await UniTask.WaitUntil(() => AllPlayersTerrainReady() || Time.time > timeout);

        if (!AllPlayersTerrainReady())
            Debug.LogWarning("[GameSceneInit] Terrain ready timeout - proceeding");

        LoadingProgress.Value = 1f;
        LoadingProgress.Complete();

        UnlockAllLocalPlayers();
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

    private void LockAllLocalPlayers()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.IsMine)
                p.LockAction();
        }
    }

    private void UnlockAllLocalPlayers()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.IsMine)
                p.UnlockAction();
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
