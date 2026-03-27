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
        if (PhotonNetwork.IsConnected)
            InitNetworkGame();
        else
            InitLocalGame();
    }

    private void InitNetworkGame()
    {
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
        if (MapSyncManager.Instance != null)
        {
            bool synced = false;
            MapSyncManager.Instance.OnMapSynced += () => synced = true;

            // GameScene에 도착한 후 마스터에게 맵 데이터 요청
            MapSyncManager.Instance.RequestMapFromMaster();

            await UniTask.WaitUntil(() => synced);
        }

        SpawnPlayer(FindSpawnPosition());
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

        // 1. 맵·지형 데이터 로드 (플레이어 데이터는 아직 적용하지 않음)
        await SaveManager.Instance.LoadAsync(slot);

        // 2. 맵 로드 후 NavMesh 빌드
        _mapNavMeshController.BuildInitialNavMesh();

        // 3. 스폰 → Start()에서 RegisterPlayer 실행됨
        SpawnPlayer(Vector3.zero);

        // 4. 한 프레임 대기 → Start() 실행 보장
        await UniTask.Yield();

        // 5. 로드된 데이터로 위치 복원
        await SaveManager.Instance.ApplyLoadedPlayers();

        // 6. 클라이언트 입장 허용
        RoomManager.Instance.OpenRoom();
    }
}