using UnityEngine;

public class TutorialNpcSpawner
{
    public static void SpawnNearPlayer(NpcDataSO data, Transform player)
    {
        if (data == null || player == null) return;

        Vector3 spawnPos = player.position + GetRandomOffset();

        var request = new NpcSpawnRequest(
            data,
            spawnPos,
            Quaternion.identity,
            null,
            false,
            "TutorialNpc");

        NpcSpawnManager.Instance.GetOrSpawn(request);
    }

    private static Vector3 GetRandomOffset()
    {
        float spawnRadius = 2.8f;
        Vector2 circle = Random.insideUnitCircle * spawnRadius;
        return new Vector3(circle.x, 0f, circle.y);
    }
}
