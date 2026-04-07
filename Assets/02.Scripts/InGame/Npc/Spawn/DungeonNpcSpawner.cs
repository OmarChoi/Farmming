using UnityEngine;

public class DungeonNpcSpawner : MonoBehaviour
{
    [SerializeField] private DungeonNpcSpawnListSO _npcSpawnList;

    // 테스트용입니다.
    [ContextMenu("Test Spawn All")]
    private void TestSpawnAll()
    {
        SpawnAll();
    }

    public void SpawnAll()
    {
        if (_npcSpawnList == null || _npcSpawnList.Entries == null) return;

        foreach (var entry in _npcSpawnList.Entries)
        {
            if (entry == null || entry.NpcData == null) continue;
            if (!CanSpawn(entry)) continue;

            SpawnNpc(entry);
        }
    }

    private bool CanSpawn(DungeonNpcSpawnEntry entry)
    {
        if (entry.AlwaysSpawn) return true;

        if (!string.IsNullOrEmpty(entry.RequiredQuestId))
        {
            return QuestManager.Instance != null && QuestManager.Instance.IsQuestCompleted(entry.RequiredQuestId);
        }

        return true;
    }

    private void SpawnNpc(DungeonNpcSpawnEntry entry)
    {
        if (!NpcLocationManager.Instance.TryGetLocation(
                entry.NpcData.NpcId,
                ENpcLocationType.Dungeon,
                entry.LocationKey,
                out Vector3 spawnPos))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"스폰 위치를 찾지 못했습니다. npc={entry.NpcData.NpcId}, key={entry.LocationKey}");
#endif
            return;
        }

        var request = new NpcSpawnRequest(
            entry.NpcData,
            spawnPos,
            Quaternion.identity,
            null,
            false,
            "DungeonNpcSpawner");

        NpcSpawnManager.Instance.GetOrSpawn(request);
#if UNITY_EDITOR
        Debug.Log($"던전 NPC 스폰 요청: {entry.NpcData.NpcName} / {spawnPos}");
#endif
    }
}
