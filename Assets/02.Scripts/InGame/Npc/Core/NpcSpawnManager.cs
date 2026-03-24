using UnityEngine;
using UnityEngine.AI;

public class NpcSpawnManager : MonoBehaviour
{
    public static NpcSpawnManager Instance { get; private set; }

    [SerializeField] private GameObject _defaultNpcPrefab;
    [SerializeField] private float _navMeshSampleRadius = 2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public NpcController GetOrSpawn(NpcSpawnRequest request)
    {
        if (!ValidateRequest(request)) return null;

        string npcId = request.NpcId;

        if (!request.ForceRespawn && NpcRegistry.Instance != null && NpcRegistry.Instance.TryGet(npcId, out var existing))
        {
            MoveExisting(existing, request);
            return existing;
        }

        if (request.ForceRespawn && NpcRegistry.Instance != null && NpcRegistry.Instance.TryGet(npcId, out var oldNpc))
        {
            Despawn(oldNpc);
        }

        return SpawnNew(request);
    }

    public void Despawn(NpcController controller)
    {
        if (controller == null) return;
        Destroy(controller.gameObject);
    }

    private bool ValidateRequest(NpcSpawnRequest request)
    {
        if (!request.HasValidData)
        {
            Debug.LogWarning("[NpcSpawnManager] 스폰 npc 데이터가 없습니다.");
            return false;
        }

        if (MapNavMeshController.Instance == null || !MapNavMeshController.Instance.IsReady)
        {
            Debug.LogWarning($"[NpcSpawnManager] NavMesh가 준비되지 않아 NPC 스폰 불가: {request.NpcId}");
            return false;
        }

        GameObject prefab = request.Prefab != null ? request.Prefab : _defaultNpcPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[NpcSpawnManager] NPC 프리팹이 없습니다: {request.NpcId}");
            return false;
        }

        return true;
    }

    private NpcController SpawnNew(NpcSpawnRequest request)
    {
        GameObject prefab = request.Prefab != null ? request.Prefab : _defaultNpcPrefab;
        Vector3 finalPos = ResolveSpawnPosition(request.RequestedPosition);

        GameObject npcObj = Instantiate(prefab, finalPos, request.Rotation, request.Parent);

        if (!npcObj.TryGetComponent(out NpcController controller))
        {
            Debug.LogWarning($"[NpcSpawnManager] NpcController가 없습니다: {request.NpcId}");
            Destroy(npcObj);
            return null;
        }

        controller.Initialize(request.Data);

        if (npcObj.TryGetComponent(out NpcMovement movement))
        {
            movement.TeleportTo(finalPos);
        }

        if (!npcObj.TryGetComponent(out NpcRuntimeIdentity identity))
        {
            identity = npcObj.AddComponent<NpcRuntimeIdentity>();
        }

        identity.Initialize(request.NpcId, controller);

        return controller;
    }

    private void MoveExisting(NpcController controller, NpcSpawnRequest request)
    {
        if (controller == null) return;

        Vector3 finalPosition = ResolveSpawnPosition(request.RequestedPosition);

        if (controller.TryGetComponent(out NpcMovement movement))
        {
            movement.TeleportTo(finalPosition);
        }
        else
        {
            controller.transform.SetPositionAndRotation(finalPosition, request.Rotation);
        }
    }

    private Vector3 ResolveSpawnPosition(Vector3 requestedPosition)
    {
        if (NavMesh.SamplePosition(requestedPosition, out NavMeshHit hit, _navMeshSampleRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return requestedPosition;
    }
}
