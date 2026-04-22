using UnityEngine;

[System.Serializable]
public class NpcScheduleEntry
{
    public GameTime ScheduleTime;
    public ENpcLocationType NpcLocationType;
    public string LocationKey;  // 필요 없으면 비워놔도 무방합니다.

    [Header("상호작용")]
    public ENpcInteractionRule InteractionRule = ENpcInteractionRule.UseDefault;

    [Header("Wandering 전용")]
    public Vector3 WanderingDirection;
    public float WanderingDistance = 20f;
    public float WanderingRadius = 8f;
    public float WanderSearchStep = 5f;
}
