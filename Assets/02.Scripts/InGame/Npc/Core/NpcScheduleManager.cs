using System.Collections.Generic;
using UnityEngine;

public class NpcScheduleManager : MonoBehaviour
{
    public static NpcScheduleManager Instance { get; private set; }

    private HashSet<NpcController> _npcs = new();

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        TimeEvents.OnMinuteChanged += HandleTimeChanged;
        TimeEvents.OnDayChanged += HandleDayChanged;
    }

    private void OnDisable()
    {
        TimeEvents.OnMinuteChanged -= HandleTimeChanged;
        TimeEvents.OnDayChanged -= HandleDayChanged;
    }

    public void Register(NpcController npc)
    {
        if (npc == null || _npcs.Contains(npc)) return;

        _npcs.Add(npc);
    }

    public void Unregister(NpcController npc)
    {
        if (npc == null) return;

        _npcs.Remove(npc);
    }

    private void HandleTimeChanged(GameTime currentGameTime)
    {
        UpdateNpcSchedules(currentGameTime);
    }

    private void UpdateNpcSchedules(GameTime currentTime)
    {
        foreach (var npc in _npcs)
        {
            if (npc.TryGetNextScheduleEntry(currentTime, out var entry))
            {
                npc.ExecuteSchedule(entry);
            }
        }
    }

    // 하루가 지나면 스케줄을 리셋합니다.
    private void HandleDayChanged(int day)
    {
        foreach (var npc in _npcs)
        {
            npc.ResetScheduleForNewDay();
        }
    }
}