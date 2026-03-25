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
        Vector3 spawnPos = Vector3.zero;

        if (PhotonNetwork.IsMasterClient)
        {
            if (NetworkManager.Instance.IsFirstVisit)
            {
                spawnPos = _mapManager.GenerateVillage();
                _mapNavMeshController.BuildInitialNavMesh();
            }
            else
            {
                LoadSaveData().Forget();
            }

            // 플레이어 즉시 스폰 (마스터는 맵이 이미 있음)
            SpawnPlayer(spawnPos);
        }
        else
        {
            // 클라이언트: 맵 동기화 완료 후 스폰
            WaitForMapAndSpawn().Forget();
        }
    }

    private async UniTaskVoid WaitForMapAndSpawn()
    {
        // MapSyncManager가 마스터로부터 맵 데이터를 받을 때까지 대기
        if (MapSyncManager.Instance != null)
        {
            bool synced = false;
            MapSyncManager.Instance.OnMapSynced += () => synced = true;

            await UniTask.WaitUntil(() => synced);
        }

        SpawnPlayer(Vector3.zero);
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

    private async UniTaskVoid LoadSaveData()
    {
        int slot = NetworkManager.Instance.SelectedSlot;
        await SaveManager.Instance.LoadAsync(slot);
    }
}