using Photon.Pun;
using UnityEngine;

public class BuildingNpcSpawner : MonoBehaviour
{
    [SerializeField] private NpcDataSO _npcData;
    [SerializeField] private Transform _spawnPoint;

    public NpcDataSO NpcData => _npcData;
    public Transform SpawnPoint => _spawnPoint != null ? _spawnPoint : transform;

    public bool HasValidData => _npcData != null && SpawnPoint != null;

    public NpcController SpawnNpc(BuildingSaveData saveData) => NpcSpawnManager.Instance.GetOrSpawn(CreateRequest(saveData));

    public NpcSpawnRequest CreateRequest(BuildingSaveData saveData)
    {
        string runtimeNpcKey = NpcRuntimeKeyUtility.CreateBuildingNpcKey(_npcData, saveData);

        // 네트워크 상황에서는 건물의 PhotonView.ViewID를 InstantiationData로 실어
        // 비마스터의 NpcController가 자기 소속 건물을 O(1)로 찾아 자가 바인딩할 수 있게 한다.
        object[] instantiationData = null;
        if (PhotonNetwork.IsConnected)
        {
            PhotonView buildingView = GetComponent<PhotonView>();
            if (buildingView != null && buildingView.ViewID != 0)
                instantiationData = new object[] { buildingView.ViewID };
        }

        return new NpcSpawnRequest(
            _npcData,
            SpawnPoint.position,
            SpawnPoint.rotation,
            null,
            false,
            "BuildingNpcSpawner",
            false,
            runtimeNpcKey,
            instantiationData);
    }
}
