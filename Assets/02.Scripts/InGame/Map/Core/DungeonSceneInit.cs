using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

public class DungeonSceneInit : MonoBehaviour
{
    [SerializeField] private int _floor = 1;

    /// 포탈에서 선택한 층 (null이면 SerializeField 사용)
    public static int? FloorOverride { get; set; }

    private const float SeedSyncTimeoutSeconds = 10f;

    private void Start()
    {
        if (FloorOverride.HasValue)
        {
            _floor = FloorOverride.Value;
            FloorOverride = null;
        }

        if (PhotonNetwork.IsConnected)
            InitNetworkDungeon();
        else
            InitLocalDungeon();
    }

    private void InitNetworkDungeon()
    {
        if (PhotonNetwork.IsMasterClient)
            InitMasterDungeon();
        else
            WaitForSeedAndGenerate().Forget();
    }

    private void InitMasterDungeon()
    {
        int seed = System.Environment.TickCount;

        // 1. 마스터가 던전 맵 생성
        MapManager.Instance.EnterDungeon(_floor, seed);

        // 2. 플레이어 배치
        PlaceAllPlayers();

        // 3. 시드만 전송 (전체 맵 데이터 대신)
        if (MapSyncManager.Instance != null)
            MapSyncManager.Instance.BroadcastDungeonSeed(_floor, seed);
    }

    private async UniTaskVoid WaitForSeedAndGenerate()
    {
        if (MapSyncManager.Instance == null) return;

        bool synced = false;
        int receivedFloor = 0;
        int receivedSeed = 0;

        MapSyncManager.Instance.OnDungeonSeedReceived += (floor, seed) =>
        {
            receivedFloor = floor;
            receivedSeed = seed;
            synced = true;
        };

        // 마스터에게 시드 요청 (브로드캐스트 누락 대비)
        MapSyncManager.Instance.RequestDungeonSeed();

        float timeout = Time.time + SeedSyncTimeoutSeconds;
        await UniTask.WaitUntil(() => synced || Time.time > timeout);

        if (!synced)
        {
            Debug.LogWarning("[DungeonSceneInit] 던전 시드 수신 타임아웃");
            return;
        }

        // 동일한 시드로 로컬에서 맵 생성
        MapManager.Instance.EnterDungeon(receivedFloor, receivedSeed);

        PlaceAllPlayers();
    }

    private void InitLocalDungeon()
    {
        int seed = System.Environment.TickCount;
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        if (players.Length > 0)
            MapManager.Instance.EnterDungeon(_floor, seed, players[0].transform);
        else
            MapManager.Instance.EnterDungeon(_floor, seed);
    }

    private void PlaceAllPlayers()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players.Length == 0) return;

        Vector3 spawnPos = FindSpawnPosition();

        foreach (var player in players)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = spawnPos;
            if (cc != null) cc.enabled = true;
        }
    }

    private Vector3 FindSpawnPosition()
    {
        var gridManager = MapManager.Instance.GridManager;
        var gridData = gridManager.GetGridData();

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

        for (int y = 20; y >= 0; y--)
        {
            if (gridData.HasCell(new Vector3Int(cx, y, cz)))
                return gridManager.GridToWorld(new Vector3Int(cx, y + 1, cz));
        }

        return Vector3.zero;
    }
}