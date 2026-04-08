using UnityEngine;

public static class TutorialNpcSpawner
{
    private const float SpawnRadius = 3.2f;

    public static NpcController SpawnNearPlayer(NpcDataSO data, Transform player)
    {
        if (data == null || player == null) return null;

        Vector3 spawnPosition = player.position + GetRandomOffset();

        var request = new NpcSpawnRequest(
            data,
            spawnPosition,
            Quaternion.identity,
            null,
            false,
            "TutorialNpc");

        return NpcSpawnManager.Instance.GetOrSpawn(request);
    }

    private static Vector3 GetRandomOffset()
    {
        Vector2 circle = Random.insideUnitCircle * SpawnRadius;
        return new Vector3(circle.x, 0f, circle.y);
    }
}
