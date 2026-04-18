using Photon.Pun;
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
        if (!ValidateRequest(request))
        {
            return null;
        }

        string runtimeNpcKey = GetSpawnKey(request);

        if (!request.ForceRespawn && NpcRegistry.Instance != null && NpcRegistry.Instance.TryGet(runtimeNpcKey, out var existing))
        {
            MoveExisting(existing, request);
            return existing;
        }

        if (request.ForceRespawn && NpcRegistry.Instance != null && NpcRegistry.Instance.TryGet(runtimeNpcKey, out var oldNpc))
        {
            Despawn(oldNpc);
        }

        return SpawnNew(request, runtimeNpcKey);
    }

    public void Despawn(NpcController controller)
    {
        if (controller == null) return;

        PhotonView view = controller.PhotonView;
        if (PhotonNetwork.IsConnected && view != null && view.IsMine)
        {
            PhotonNetwork.Destroy(controller.gameObject);
        }
        else
        {
            Destroy(controller.gameObject);
        }
    }

    private string GetSpawnKey(NpcSpawnRequest request)
    {
        if (!string.IsNullOrEmpty(request.RuntimeNpcKey)) return request.RuntimeNpcKey;
        return request.NpcId;
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
            if (request.Reason != "TutorialNpc")
            {
                Debug.LogWarning($"[NpcSpawnManager] NavMesh가 준비되지 않아 NPC 스폰 불가: {request.NpcId}");
                return false;
            }
        }

        GameObject prefab = request.Prefab != null ? request.Prefab : _defaultNpcPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[NpcSpawnManager] NPC 프리팹이 없습니다: {request.NpcId}");
            return false;
        }

        return true;
    }

    private NpcController SpawnNew(NpcSpawnRequest request, string runtimeNpcKey)
    {
        GameObject prefab = request.Prefab != null ? request.Prefab : _defaultNpcPrefab;
        Vector3 finalPosition = ResolveInitialSpawnPosition(request, runtimeNpcKey);

        GameObject npcObject;
        if (PhotonNetwork.IsConnected && !request.IsLocalOnly)
        {
            // 네트워크 NPC는 등록된 AssetKey만 Photon prefabId로 사용할 수 있게 검증한다.
            string prefabKey = AssetKey.NetworkPrefab.GetKey(prefab);
            if (string.IsNullOrEmpty(prefabKey))
            {
                Debug.LogError($"[NpcSpawnManager] {prefab.name} Npc doesn't exist in AssetKey.NetworkPrefab");
                return null;
            }

            npcObject = PhotonNetwork.Instantiate(prefabKey, finalPosition, request.Rotation);
            if (npcObject == null) return null;
        }
        else
        {
            npcObject = Instantiate(prefab, finalPosition, request.Rotation, request.Parent);
        }

        if (!npcObject.TryGetComponent(out NpcController controller))
        {
            Debug.LogWarning($"[NpcSpawnManager] NpcController가 없습니다: {request.NpcId}");
            Destroy(npcObject);
            return null;
        }

        controller.Initialize(request.Data, request.IsLocalOnly, runtimeNpcKey);
        controller.SetInitialScheduleBase(finalPosition);

        if (npcObject.TryGetComponent(out NpcMovement movement))
        {
            movement.TeleportTo(finalPosition);
        }
        if (!npcObject.TryGetComponent(out NpcRuntimeIdentity identity))
        {
            identity = npcObject.AddComponent<NpcRuntimeIdentity>();
        }

        identity.Initialize(runtimeNpcKey, controller);
        controller.SyncScheduleToCurrentTime();

        return controller;
    }

    // 우선 스폰 위치는 Home을 기준으로 합니다.
    private Vector3 ResolveInitialSpawnPosition(NpcSpawnRequest request, string runtimeNpcKey)
    {
        if (request.Reason == "TutorialNpc")
        {
            return ResolveSpawnPosition(request.RequestedPosition);
        }

        if (NpcLocationManager.Instance != null &&
            NpcLocationManager.Instance.TryGetLocation(
                runtimeNpcKey,
                ENpcLocationType.Home,
                string.Empty,
                out Vector3 homePosition))
        {
            return ResolveSpawnPosition(homePosition);
        }

        return ResolveSpawnPosition(request.RequestedPosition);
    }

    private void MoveExisting(NpcController controller, NpcSpawnRequest request)
    {
        if (controller == null) return;

        string runtimeNpcKey = GetSpawnKey(request);
        Vector3 finalPosition = ResolveInitialSpawnPosition(request, runtimeNpcKey);

        if (controller.TryGetComponent(out NpcMovement movement))
        {
            movement.TeleportTo(finalPosition);
        }
        else
        {
            controller.transform.SetPositionAndRotation(finalPosition, request.Rotation);
        }

        controller.SetInitialScheduleBase(finalPosition);
        controller.SyncScheduleToCurrentTime();
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
