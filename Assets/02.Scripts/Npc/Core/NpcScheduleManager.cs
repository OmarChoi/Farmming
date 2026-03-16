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
        // 테스트용으로 TestTimeManager의 이벤트에 구독합니다.
        // 실제 게임에서는 TimeManager가 완성된 후 이 부분을 수정해야 할 수 있습니다.
        if (TestTimeManager.Instance != null)
        {
            TestTimeManager.Instance.OnTimeChanged += HandleTimeChanged;
            TestTimeManager.Instance.OnDayChanged += HandleDayChanged;
        }
    }

    private void OnEnable()
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

    // 시간에 따른 이동 구현 테스트용 메서드입니다.
    // todo. TimeManager에서 시간이 변경될 때마다 이 메서드가 호출되도록 구현해야 합니다.
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