using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;

public class TroublemakerSpawner : MonoBehaviour
{
    [Header("방해꾼 스폰 수")]
    [SerializeField] private int _minSpawnCount = 3;
    [SerializeField] private int _maxSpawnCount = 5;

    private readonly List<TroublemakerController> _spawnedTroublemakers = new();

    public void SpawnAll(TroublemakerSpawnListSO spawnList, int floor, int seed)
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;
        if (spawnList == null || spawnList.Entries == null) return;

        List<TroublemakerSpawnEntry> spawnableEntries = GetSpawnableEntries(spawnList);
        if (spawnableEntries.Count == 0) return;

        var random = new System.Random(seed);
        SpawnByMode(spawnableEntries, random);
    }

    private List<TroublemakerSpawnEntry> GetSpawnableEntries(TroublemakerSpawnListSO spawnList)
    {
        List<TroublemakerSpawnEntry> result = new();

        foreach (var entry in spawnList.Entries)
        {
            if (entry == null || entry.Data == null) continue;
            if (!CanSpawn(entry)) continue;

            result.Add(entry);
        }

        return result;
    }

    private void SpawnByMode(List<TroublemakerSpawnEntry> entries, System.Random random)
    {
        var fixedAnchorEntries = FilterByMode(entries, ETroublemakerSpawnMode.FixedAnchor);
        var randomCellEntries = FilterByMode(entries, ETroublemakerSpawnMode.RandomCell);
        var nearestPlayerEntries = FilterByMode(entries, ETroublemakerSpawnMode.NearestPlayer);
        var roomCenterEntries = FilterByMode(entries, ETroublemakerSpawnMode.RoomCenter);

        if (fixedAnchorEntries.Count > 0)
        {
            // SpawnByFixedAnchor(fixedAnchorEntries);
        }
        if (randomCellEntries.Count > 0)
        {
            SpawnByRandomCell(randomCellEntries, random);
        }
        if (nearestPlayerEntries.Count > 0)
        {
            // SpawnByNearestPlayer(nearestPlayerEntries);
        }
        if (roomCenterEntries.Count > 0)
        {
            // SpawnByRoomCenter(roomCenterEntries);
        }
    }

    private List<TroublemakerSpawnEntry> FilterByMode(List<TroublemakerSpawnEntry> entries, ETroublemakerSpawnMode mode)
    {
        return entries.Where(e => e.SpawnMode == mode).ToList();
    }

    private void SpawnByRandomCell(List<TroublemakerSpawnEntry> randomCellEntries, System.Random random)
    {
        if (randomCellEntries == null || randomCellEntries.Count == 0) return;

        List<TerrainCell> candidates = FindRandomCellCandidates();
        if (candidates.Count == 0) return;

        Shuffle(candidates, random);

        int desiredCount = random.Next(_minSpawnCount, _maxSpawnCount + 1);
        int spawnCount = Mathf.Min(desiredCount, candidates.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            TerrainCell cell = candidates[i];

            if (!TryGetSpawnPositionFromCell(cell, out Vector3 spawnPosition))
            {
                Debug.LogWarning($"[TroublemakerSpawner] 셀 스폰 위치 계산 실패");
                continue;
            }

            int entryIndex = random.Next(randomCellEntries.Count);
            TroublemakerSpawnEntry entry = randomCellEntries[entryIndex];

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

    private List<TerrainCell> FindRandomCellCandidates()
    {
        List<TerrainCell> candidates = new();

        var gridManager = MapManager.Instance.GridManager;
        if (gridManager == null) return candidates;

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

            float sqrDistance = (cell.transform.position - playerPosition).sqrMagnitude;
            if (sqrDistance < sqrMin || sqrDistance > sqrMax) continue;

            candidates.Add(cell);
        }

        return candidates;
    }

    private bool TryGetSpawnPositionFromCell(TerrainCell cell, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        if (cell == null) return false;

        var gridManager = MapManager.Instance.GridManager;
        if (gridManager == null) return false;

        Vector3 rawPosition = gridManager.GridToWorld(cell.GridPosition + Vector3Int.up);

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

        GameObject obj;
        if (PhotonNetwork.IsConnected)
        {
            obj = PhotonNetwork.Instantiate(entry.Prefab.name, spawnPosition, Quaternion.identity);
        }
        else
        {
            obj = Instantiate(entry.Prefab, spawnPosition, Quaternion.identity);
        }

        if (!obj.TryGetComponent(out TroublemakerController controller))
        {
            Debug.LogWarning($"[TroublemakerSpawner] TroublemakerController가 없습니다. id={entry.TroublemakerId}");
            Destroy(obj);
            return;
        }

        controller.Initialize(entry.Data, spawnPosition);
        _spawnedTroublemakers.Add(controller);
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

    private void Shuffle<T>(List<T> list, System.Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void DespawnAll()
    {
        for (int i = _spawnedTroublemakers.Count - 1; i >= 0; i--)
        {
            TroublemakerController controller = _spawnedTroublemakers[i];
            if (controller == null) continue;

            if (PhotonNetwork.IsConnected)
            {
                PhotonView view = controller.GetComponent<PhotonView>();
                if (view != null && PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(controller.gameObject);
                }
            }
            else
            {
                Destroy(controller.gameObject);
            }
        }

        _spawnedTroublemakers.Clear();
    }
}
