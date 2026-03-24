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

        string npcId = request.Data.NpcId;

        if (!request.ForceRespawn &&
            NpcRegistry.Instance != null &&
            NpcRegistry.Instance.TryGet(npcId, out var existing))
        {
            MoveExisting(existing, request);
            return existing;
        }

        if (request.ForceRespawn &&
            NpcRegistry.Instance != null &&
            NpcRegistry.Instance.TryGet(npcId, out var oldNpc))
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
        if (request.Data == null)
        {
            Debug.LogWarning("[NpcSpawnManager] Spawn failed: data is null");
            return false;
        }

        if (MapNavMeshController.Instance == null || !MapNavMeshController.Instance.IsReady)
        {
            Debug.LogWarning($"[NpcSpawnManager] NavMesh not ready. Cannot spawn NPC: {request.Data.NpcId}");
            return false;
        }

        GameObject prefab = request.Data.Prefab != null ? request.Data.Prefab : _defaultNpcPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[NpcSpawnManager] No prefab assigned for NPC: {request.Data.NpcId}");
            return false;
        }

        return true;
    }

    private NpcController SpawnNew(NpcSpawnRequest request)
    {
        GameObject prefab = request.Data.Prefab != null ? request.Data.Prefab : _defaultNpcPrefab;
        Vector3 finalPos = ResolveSpawnPosition(request.RequestedPosition);

        GameObject npcObj = Instantiate(prefab, finalPos, request.Rotation, request.Parent);

        if (!npcObj.TryGetComponent(out NpcController controller))
        {
            Debug.LogWarning($"[NpcSpawnManager] Spawned prefab has no NpcController: {request.Data.NpcId}");
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

        identity.Initialize(request.Data.NpcId, controller);

        return controller;
    }

    private void MoveExisting(NpcController controller, NpcSpawnRequest request)
    {
        if (controller == null) return;

        Vector3 finalPos = ResolveSpawnPosition(request.RequestedPosition);

        if (controller.TryGetComponent(out NpcMovement movement))
        {
            movement.TeleportTo(finalPos);
        }
        else
        {
            controller.transform.SetPositionAndRotation(finalPos, request.Rotation);
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
