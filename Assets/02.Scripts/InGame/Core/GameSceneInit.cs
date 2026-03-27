using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

public class GameSceneInit : MonoBehaviour
{
    [SerializeField] private string _playerPrefabName = "Player";
    [SerializeField] private MapManager _mapManager;
    [SerializeField] private MapNavMeshController _mapNavMeshController;

    private void Start()
    {
        Debug.Log($"[GameSceneInit] Start — IsConnected={PhotonNetwork.IsConnected}, InRoom={PhotonNetwork.InRoom}, IsMaster={PhotonNetwork.IsMasterClient}");

        if (PhotonNetwork.IsConnected)
            InitNetworkGame();
        else
            InitLocalGame();
    }

    private void InitNetworkGame()
    {
        Debug.Log($"[GameSceneInit] InitNetworkGame — IsMaster={PhotonNetwork.IsMasterClient}");

        if (PhotonNetwork.IsMasterClient)
        {
            if (RoomManager.Instance.IsFirstVisit)
            {
                Vector3 spawnPos = _mapManager.GenerateVillage();
                _mapNavMeshController.BuildInitialNavMesh();
                SpawnPlayer(spawnPos);
                RoomManager.Instance.OpenRoom();
            }
            else
            {
                LoadAndSpawnMaster().Forget();
            }
        }
        else
        {
            // 클라이언트: 맵 동기화 완료 후 스폰
            WaitForMapAndSpawn().Forget();
        }
    }

    private async UniTaskVoid WaitForMapAndSpawn()
    {
        try
        {
            Debug.Log($"[Client] WaitForMapAndSpawn 시작 — InRoom={PhotonNetwork.InRoom}, MapSync={MapSyncManager.Instance != null}");

            if (!PhotonNetwork.InRoom)
            {
                Debug.LogError("[Client] 방에 입장하지 않은 상태 — 스폰 불가");
                return;
            }

            if (MapSyncManager.Instance != null)
            {
                bool synced = false;
                MapSyncManager.Instance.OnMapSynced += () => synced = true;
                MapSyncManager.Instance.RequestMapFromMaster();

                Debug.Log("[Client] 맵 동기화 요청 완료, 대기 중...");

                float timeout = Time.time + 10f;
                await UniTask.WaitUntil(() => synced || Time.time > timeout);

                Debug.Log($"[Client] 대기 종료 — synced={synced}");

                if (!synced)
                {
                    Debug.LogError("[Client] 맵 동기화 시간 초과");
                    return;
                }
            }
            else
            {
                Debug.LogError("[Client] MapSyncManager.Instance가 null");
                return;
            }

            var pos = FindSpawnPosition();
            Debug.Log($"[Client] 스폰 위치: {pos}");
            SpawnPlayer(pos);
            Debug.Log("[Client] 플레이어 스폰 완료");

            // 1초 후 존재 확인
            await UniTask.Delay(1000);
            var allPlayers = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            Debug.Log($"[Client] 1초 후 PlayerController 수: {allPlayers.Length}");
            foreach (var p in allPlayers)
                Debug.Log($"  - {p.name}, IsMine={p.IsMine}, pos={p.transform.position}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Client] WaitForMapAndSpawn 실패: {e}");
        }
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

        for (int y = 20; y >= 0; y--)
        {
            if (gridData.HasCell(new Vector3Int(cx, y, cz)))
                return gridManager.GridToWorld(new Vector3Int(cx, y + 1, cz));
        }

        return Vector3.zero;
    }

    private void SpawnPlayer(Vector3 spawnPos)
    {
        var playerObj = PhotonNetwork.Instantiate(_playerPrefabName, spawnPos, Quaternion.identity);
        var pv = playerObj.GetPhotonView();
        Debug.Log($"[SpawnPlayer] name={playerObj.name}, active={playerObj.activeSelf}, ViewID={pv?.ViewID}, IsMine={pv?.IsMine}, scene={playerObj.scene.name}");
        var pc = playerObj.GetComponent<PlayerController>();

        if (CustomizeData.Instance != null)
            pc.GetAbility<PlayerCustomizeAbility>()?.Initialize(CustomizeData.Instance.Data);
    }

    private void InitLocalGame()
    {
        var existing = FindAnyObjectByType<PlayerController>();

        Vector3 spawnPos = _mapManager.GenerateVillage(existing != null ? existing.transform : null);
        _mapNavMeshController.BuildInitialNavMesh();

        if (existing != null && CustomizeData.Instance != null)
            existing.GetAbility<PlayerCustomizeAbility>()?.Initialize(CustomizeData.Instance.Data);
    }

    private async UniTaskVoid LoadAndSpawnMaster()
    {
        int slot = RoomManager.Instance.SelectedSlot;

        // 1. 맵·지형 데이터 로드 → _loadedData 보관
        await SaveManager.Instance.LoadAsync(slot);

        // 2. 맵 로드 후 NavMesh 빌드
        _mapNavMeshController.BuildInitialNavMesh();

        // 3. 스폰 → Start()에서 RegisterPlayer → TryRestorePlayer 자동 복원
        SpawnPlayer(Vector3.zero);

        // 4. 한 프레임 대기 → Start() 실행 보장
        await UniTask.Yield();

        // 5. 클라이언트 입장 허용
        RoomManager.Instance.OpenRoom();
    }
}