using UnityEngine;

public static class TutorialNpcSpawner
{
    private const float SpawnRadius = 3.8f;
    private const float FirstTutorialSpawnRadius = 8.2f;

    private const int MaxSpawnTryCount = 4;
    private const float GroundCheckStartHeight = 10f;
    private const float GroundCheckDistance = 20f;
    private const float GroundOffsetY = 0.05f;

    public static NpcController SpawnNearPlayer(NpcDataSO data, PlayerController player, bool isFirstTutorialQuestCompleted)
    {
        if (data == null || player == null || NpcSpawnManager.Instance == null) return null;
        float radius = isFirstTutorialQuestCompleted ? SpawnRadius : FirstTutorialSpawnRadius;

        Vector3 spawnPosition = FindSpawnPosition(player.transform.position, radius);

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

    private static Vector3 GetRandomOffset(float radius)
    {
        Vector2 circle = Random.insideUnitCircle * radius;
        return new Vector3(circle.x, 0f, circle.y);
    }

    private static Vector3 FindSpawnPosition(Vector3 playerPosition, float radius)
    {
        for (int i = 0; i < MaxSpawnTryCount; i++)
        {
            Vector3 candidate = playerPosition + GetRandomOffset(radius);

            if (TryResolveGroundPosition(candidate, out Vector3 groundedPosition))
            {
                return groundedPosition;
            }
        }

        // 실패 시 플레이어 높이를 기준으로 fallback합니다.
        return playerPosition;
    }

    private static bool TryResolveGroundPosition(Vector3 position, out Vector3 groundedPosition)
    {
        Vector3 rayOrigin = new Vector3(position.x, position.y + GroundCheckStartHeight, position.z);

        if (Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            GroundCheckDistance,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            groundedPosition = new Vector3(position.x, hit.point.y + GroundOffsetY, position.z);
            return true;
        }

        groundedPosition = position;
        return false;
    }
}
