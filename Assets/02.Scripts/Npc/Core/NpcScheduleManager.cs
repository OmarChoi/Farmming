using System.Collections.Generic;
using UnityEngine;

public class NpcScheduleManager : MonoBehaviour
{
    public static NpcScheduleManager Instance { get; private set; }

    private readonly List<NpcController> _npcs = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (TestTimeManager.Instance != null)
        {
            TestTimeManager.Instance.OnTimeChanged += HandleTimeChanged;
            TestTimeManager.Instance.OnDayChanged += HandleDayChanged;
        }
    }

    private void OnDisable()
    {
        if (TestTimeManager.Instance != null)
        {
            TestTimeManager.Instance.OnTimeChanged -= HandleTimeChanged;
            TestTimeManager.Instance.OnDayChanged -= HandleDayChanged;
        }
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

    // 시간에 따른 이동 구현용 메서드입니다. (임시)
    private void HandleTimeChanged(int currentTime)
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
    private void HandleDayChanged()
    {
        foreach (var npc in _npcs)
        {
            npc.ResetScheduleForNewDay();
        }
    }
}