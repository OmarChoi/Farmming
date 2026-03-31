using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

public class DungeonSceneInit : MonoBehaviour
{
    [SerializeField] private int _floor = 1;

    private const float MapSyncTimeoutSeconds = 10f;

    private void Start()
    {
        if (PhotonNetwork.IsConnected)
            InitNetworkDungeon();
        else
            InitLocalDungeon();
    }

    private void InitNetworkDungeon()
    {
        if (PhotonNetwork.IsMasterClient)
            InitMasterDungeon().Forget();
        else
            WaitForMapSync().Forget();
    }

    private async UniTaskVoid InitMasterDungeon()
    {
        // 1. 마스터가 던전 맵 생성
        MapManager.Instance.EnterDungeon(_floor);

        // 2. 플레이어 배치
        PlaceAllPlayers();

        // 4. 한 프레임 대기 후 클라이언트에 맵 전송
        await UniTask.Yield();

        if (MapSyncManager.Instance != null)
            MapSyncManager.Instance.BroadcastMap();
    }

    private async UniTaskVoid WaitForMapSync()
    {
        if (MapSyncManager.Instance == null) return;

        bool synced = false;
        MapSyncManager.Instance.OnMapSynced += () => synced = true;
        MapSyncManager.Instance.RequestMapFromMaster();

        float timeout = Time.time + MapSyncTimeoutSeconds;
        await UniTask.WaitUntil(() => synced || Time.time > timeout);

        if (!synced)
        {
            Debug.LogWarning("[DungeonSceneInit] 맵 동기화 타임아웃");
            return;
        }

        // 클라이언트는 NavMesh 빌드 안 함 (마스터에서 NPC AI 실행)
        PlaceAllPlayers();
    }

    private void InitLocalDungeon()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        if (players.Length > 0)
            MapManager.Instance.EnterDungeon(_floor, players[0].transform);
        else
            MapManager.Instance.EnterDungeon(_floor);

    }

    private void PlaceAllPlayers()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players.Length == 0) return;

        // 맵 중앙 스폰 위치 계산
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