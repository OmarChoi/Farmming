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

        return new NpcSpawnRequest(
            _npcData,
            SpawnPoint.position,
            SpawnPoint.rotation,
            null,
            false,
            "BuildingNpcSpawner",
            false,
            runtimeNpcKey);
    }
}
