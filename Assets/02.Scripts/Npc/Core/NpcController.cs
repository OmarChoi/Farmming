using UnityEngine;

public class NpcController : MonoBehaviour
{
    [Header("Npc 데이터 관련")]
    [SerializeField] private NpcData _npcData;
    [SerializeField] private NpcSchedule _npcSchedule;

    private int _timeOffset;
    private int _currentScheduleIndex = 0;

    private NpcMovement _movement;

    public NpcData Data => _npcData;
    public NpcSchedule Schedule => _npcSchedule;

    private void Awake()
    {
        _movement = GetComponent<NpcMovement>();

        GenerateTimeOffset();
    }

    private void Start()
    {
        NpcScheduleManager.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (NpcScheduleManager.Instance != null)
        {
            NpcScheduleManager.Instance.Unregister(this);
        }
    }

    // Npc가 스케줄대로 이동하는 시간에 랜덤 변수를 지정해줍니다. (스케줄이 겹치는 npc가 동시에 이동하는 것 방지합니다.)
    private void GenerateTimeOffset()
    {
        if (_npcData != null && _npcData.UseRandomTimeOffset)
        {
            _timeOffset = Random.Range(_npcData.MinTimeOffset, _npcData.MaxTimeOffset + 1);
        }
        else
        {
            _timeOffset = 0;
        }
    }

    // 시간이 되면 Npc가 다음 일정대로 움직이는 것을 시도합니다.
    public bool TryGetNextScheduleEntry(int time, out NpcScheduleEntry entry)
    {
        entry = null;

        if (_npcSchedule == null || _npcSchedule.ScheduleEntries == null)
        {
            return false;
        }

        if (_currentScheduleIndex >= _npcSchedule.ScheduleEntries.Count)
        {
            return false;
        }

        NpcScheduleEntry nextEntry = _npcSchedule.ScheduleEntries[_currentScheduleIndex];
        int scheduleTime = nextEntry.ScheduleTime + _timeOffset;

        if (time >= scheduleTime)
        {
            entry = nextEntry;
            _currentScheduleIndex++;
            return true;
        }

        return false;
    }

    // 일정이 있다면 스케줄대로 행동을 실행합니다.
    public void ExecuteSchedule(NpcScheduleEntry entry)
    {
        // todo. 추후 진짜 건설 관련에 연결
        Vector3 targetPosition = TestBuildingManager.Instance.GetLocationPosition(_npcData.NpcId, entry.NpcLocationType);

        _movement.MoveTo(targetPosition);

        Debug.Log($"{_npcData.NpcName}가 이동합니다: {entry.NpcLocationType}");
    }

    // 하루가 지나면 스케줄을 리셋합니다.
    public void ResetScheduleForNewDay()
    {
        _currentScheduleIndex = 0;
        GenerateTimeOffset();

        if (_npcSchedule == null || _npcSchedule.ScheduleEntries == null || _npcSchedule.ScheduleEntries.Count == 0) return;

        NpcScheduleEntry firstEntry = _npcSchedule.ScheduleEntries[0];
        Vector3 startPosition = TestBuildingManager.Instance.GetLocationPosition(_npcData.NpcId, firstEntry.NpcLocationType);

        _movement.TeleportTo(startPosition);
    }
}
