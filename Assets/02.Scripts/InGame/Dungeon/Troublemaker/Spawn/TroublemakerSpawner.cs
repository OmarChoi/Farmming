using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TroublemakerSpawner : MonoBehaviour
{
    [SerializeField] private TroublemakerSpawnListSO _spawnList;

    public void SpawnAll(int floor, int seed)
    {
        if (_spawnList == null || _spawnList.Entries == null) return;

        foreach (var entry in _spawnList.Entries)
        {
            if (entry == null || entry.Data == null) continue;
            if (!CanSpawn(entry)) continue;

            if (!TryResolveSpawnPosition(entry, floor, seed, out Vector3 spawnPosition))
            {
                Debug.LogWarning($"[TroublemakerSpawner] 스폰 위치를 찾지 못했습니다. id={entry.Data.TroublemakerId}");
                continue;
            }

            Spawn(entry, spawnPosition);
        }
    }

    private bool CanSpawn(TroublemakerSpawnEntry entry)
    {
        if (entry.AlwaysSpawn) return true;

        if (!string.IsNullOrEmpty(entry.RequiredQuestId))
        {
            return QuestManager.Instance != null && QuestManager.Instance.IsQuestCompleted(entry.RequiredQuestId);
        }

        return true;
    }

    private bool TryResolveSpawnPosition(TroublemakerSpawnEntry entry, int floor, int seed, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        switch (entry.SpawnMode)
        {
            case ETroublemakerSpawnMode.FixedAnchor:
                return NpcLocationManager.Instance != null &&
                       NpcLocationManager.Instance.TryGetLocation(
                           entry.TroublemakerId,
                           ENpcLocationType.Dungeon,
                           entry.LocationKey,
                           out spawnPosition);

            case ETroublemakerSpawnMode.RandomCell:
                return TryFindRandomCellSpawnPosition(floor, seed, out spawnPosition);
        }

        return false;
    }

    private bool TryFindRandomCellSpawnPosition(int floor, int seed, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        var gridManager = MapManager.Instance.GridManager;
        if (gridManager == null) return false;

        List<TerrainCell> candidates = new();

        Vector3 playerPosition = GetPlayerPosition();

        float minDistance = 5f;
        float maxDistance = 20f;
        float sqrMin = minDistance * minDistance;
        float sqrMax = maxDistance * maxDistance;

        foreach (var kvp in gridManager.Cells)
        {
            TerrainCell cell = kvp.Value;
            if (cell == null) continue;
            if (!cell.Data.IsTop) continue;
            if (cell.Data.ObjectType != EGridObjectType.None) continue;
            if (cell.Data.TileType == ETileType.Dungeon3Lava) continue;
            if (!cell.HasObjectPoint) continue;

            float sqrDistance = (cell.transform.position - playerPosition).sqrMagnitude;
            if (sqrDistance < sqrMin || sqrDistance > sqrMax) continue;

            candidates.Add(cell);
        }

        if (candidates.Count == 0) return false;

        var random = new System.Random(seed);
        int index = random.Next(candidates.Count);

        Vector3 rawPosition = gridManager.GridToWorld(candidates[index].GridPosition + Vector3Int.up);

        if (NavMesh.SamplePosition(rawPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            spawnPosition = hit.position;
            return true;
        }

        return false;
    }

    private void Spawn(TroublemakerSpawnEntry entry, Vector3 spawnPosition)
    {
        if (entry.Prefab == null)
        {
            Debug.LogWarning($"[TroublemakerSpawner] Prefab이 없습니다. id={entry.TroublemakerId}");
            return;
        }

        GameObject obj = Instantiate(entry.Prefab, spawnPosition, Quaternion.identity);

        if (!obj.TryGetComponent(out TroublemakerController controller))
        {
            Debug.LogWarning($"[TroublemakerSpawner] TroublemakerController가 없습니다. id={entry.TroublemakerId}");
            Destroy(obj);
            return;
        }

        controller.Initialize(entry.Data, spawnPosition);
    }

    private Vector3 GetPlayerPosition()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (var p in players)
        {
            if (p.IsMine)
            {
                return p.transform.position;
            }

        }
        return Vector3.zero;
    }
}
