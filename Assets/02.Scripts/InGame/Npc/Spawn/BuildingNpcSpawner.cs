using UnityEngine;

public class BuildingNpcSpawner : MonoBehaviour
{
    [SerializeField] private NpcDataSO _npcData;
    [SerializeField] private Transform _spawnPoint;

    public NpcDataSO NpcData => _npcData;
    public Transform SpawnPoint => _spawnPoint != null ? _spawnPoint : transform;

    public bool HasValidData => _npcData != null && SpawnPoint != null;

    public void SpawnNpc() => NpcSpawnManager.Instance.GetOrSpawn(CreateRequest());
    
    public NpcSpawnRequest CreateRequest()
    {
        return new NpcSpawnRequest(
            _npcData,
            SpawnPoint.position,
            SpawnPoint.rotation,
            null,
            false,
            "BuildingNpcSpawner");
    }
}
