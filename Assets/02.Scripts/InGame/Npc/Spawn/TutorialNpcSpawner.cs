using UnityEngine;

public static class TutorialNpcSpawner
{
    private const float SpawnRadius = 4.4f;
    private const float FirstTutorialSpawnRadius = 28.6f;

    private const int MaxSpawnTryCount = 8;
    private const float GroundCheckStartHeight = 20f;
    private const float GroundCheckDistance = 40f;
    private const float GroundOffsetY = 0.05f;

    private static readonly int GroundLayerMask = LayerMask.GetMask("Default");

    private const float FullCircle = Mathf.PI * 2f;
    private const float MinSpawnRadiusRatio = 0.8f;

    public static NpcController SpawnNearPlayer(NpcDataSO data, PlayerController player, bool isFirstTutorialQuestCompleted)
    {
        if (data == null || player == null || NpcSpawnManager.Instance == null) return null;

        float radius = isFirstTutorialQuestCompleted ? SpawnRadius : FirstTutorialSpawnRadius;

        Vector3 basePosition = player.transform.position;
        if (TryResolveGroundPosition(basePosition, out Vector3 groundedBasePosition))
        {
            basePosition = groundedBasePosition;
        }

        Vector3 spawnPosition = FindSpawnPosition(basePosition, radius);

        var request = new NpcSpawnRequest(
            data,
            spawnPosition,
            Quaternion.identity,
            null,
            true,
            "TutorialNpc",
            true);

        return NpcSpawnManager.Instance.GetOrSpawn(request);
    }

    private static Vector3 FindSpawnPosition(Vector3 basePosition, float radius)
    {
        // 반경 내에서 랜덤한 위치를 시도하여 지면에 닿는지 확인합니다.
        for (int i = 0; i < MaxSpawnTryCount; i++)
        {
            Vector3 candidate = basePosition + GetRandomOffset(radius);

            if (TryResolveGroundPosition(candidate, out Vector3 groundedPosition))
            {
                return groundedPosition;
            }
        }

        // 모든 시도가 실패하면 basePosition에서 수직으로 raycast하여 fallback합니다.
        if (TryResolveGroundPosition(basePosition, out Vector3 fallbackGroundedPosition))
        {
            return fallbackGroundedPosition;
        }

        // 그래도 실패하면 basePosition을 그대로 반환합니다.
        return basePosition;
    }

    private static Vector3 GetRandomOffset(float radius)
    {
        float angle = Random.Range(0f, FullCircle);

        float t = Random.value;
        t = t * t; // 바깥쪽에 더 많이 퍼지게 합니다.

        float finalRadius = Mathf.Lerp(radius * MinSpawnRadiusRatio, radius, t);
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * finalRadius;
    }

    private static bool TryResolveGroundPosition(Vector3 position, out Vector3 groundedPosition)
    {
        Vector3 rayOrigin = new Vector3(position.x, position.y + GroundCheckStartHeight, position.z);

        if (Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            GroundCheckDistance,
            GroundLayerMask,
            QueryTriggerInteraction.Ignore))
        {
            groundedPosition = new Vector3(position.x, hit.point.y + GroundOffsetY, position.z);
            return true;
        }

        groundedPosition = position;
        return false;
    }
}
