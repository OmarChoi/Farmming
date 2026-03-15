using UnityEngine;

public class NpcController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private NpcData _npcData;
    [SerializeField] private NpcSchedule _npcSchedule;

    private NpcMovement _movement;

    public NpcData Data => _npcData;
    public NpcSchedule Schedule => _npcSchedule;

    private void Awake()
    {
        _movement = GetComponent<NpcMovement>();
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

    public bool TryGetScheduleEntry(int time, out NpcScheduleEntry entry)
    {
        entry = null;

        if (_npcSchedule == null || _npcSchedule.ScheduleEntries == null)
        {
            return false;
        }

        for (int i = 0; i < _npcSchedule.ScheduleEntries.Count; i++)
        {
            if (_npcSchedule.ScheduleEntries[i].ScheduleTime == time)
            {
                entry = _npcSchedule.ScheduleEntries[i];
                return true;
            }
        }

        return false;
    }
}
