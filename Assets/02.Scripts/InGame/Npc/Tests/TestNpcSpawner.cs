using UnityEngine;
using System.Collections;

public class TestNpcSpawner : MonoBehaviour
{
    [SerializeField] private NpcDataContainerSO _npcDatabase;
    [SerializeField] private string _npcId;

    private const float SpawnDelay = 4f;

    private void Start()
    {
        NpcDataSO data = _npcDatabase.GetNpc(_npcId);
        if (data == null)
        {
            Debug.LogWarning($"npc가 없습니다: {_npcId}");
            return;
        }

        var request = new NpcSpawnRequest(
            data,
            transform.position,
            transform.rotation,
            null,
            false,
            "TestNpcSpawner");

        StartCoroutine(TestSpawnNpc(request));
    }

    private IEnumerator TestSpawnNpc(NpcSpawnRequest request)
    {
        yield return new WaitForSeconds(SpawnDelay);
        NpcSpawnManager.Instance.GetOrSpawn(request);
    }
}
