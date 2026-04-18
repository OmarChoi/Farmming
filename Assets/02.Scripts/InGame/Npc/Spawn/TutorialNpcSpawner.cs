using UnityEngine;

public static class TutorialNpcSpawner
{
    [Header("스폰 범위 설정")]
    [SerializeField] private static float _spawnRadius = 3.8f;
    [SerializeField] private static float _firstTutorialSpawnRadius = 8.2f;

    private static ETutorialState _state;

    public static NpcController SpawnNearPlayer(NpcDataSO data, PlayerController player)
    {
        if (data == null || player == null || NpcSpawnManager.Instance == null) return null;

        _state = player.GetAbility<PlayerQuestAbility>()?.TutorialState ?? ETutorialState.None;

        Vector3 spawnPosition = player.transform.position + GetRandomOffset(SetSpawnRadius(_state));

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

    private static float SetSpawnRadius(ETutorialState state)
    {
        switch(state){
            case ETutorialState.None:
                return _firstTutorialSpawnRadius;
            case ETutorialState.InProgress:
            case ETutorialState.Completed:
                return _spawnRadius;
            default:
                return _spawnRadius;
        }
    }

    private static Vector3 GetRandomOffset(float radius)
    {
        Vector2 circle = Random.insideUnitCircle * radius;
        return new Vector3(circle.x, 0f, circle.y);
    }
}
