using UnityEngine;

public class NpcController : MonoBehaviour
{
    [Header("Npc 컴포넌트")]
    [SerializeField] private Animator _animator;
    [SerializeField] private NpcMovement _movement;
    [SerializeField] private NpcAnimatorController _anim;

    [Header("Npc 데이터")]
    [SerializeField] private NpcScheduleSO _npcSchedule;
    [SerializeField] private NpcInteractionOption[] _interactionOptions;

    [Header("상점 옵션")]
    [SerializeField] private Shop _shop;

    private NpcDataSO _npcData;
    private Transform _currentInteractor;

    private int _timeOffset;
    private int _currentScheduleIndex = 0;

    private bool _isInteracting;

    public Animator Animator => _animator;
    public NpcAnimatorController Anim => _anim;
    public NpcDataSO Data => _npcData;
    public NpcScheduleSO Schedule => _npcSchedule;
    public Shop Shop => _shop;
    public Transform CurrentInteractor => _currentInteractor;
    public NpcInteractionOption[] InteractionOptions => _interactionOptions;
    public bool IsInteracting => _isInteracting;

    private void Awake()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_movement == null) _movement = GetComponent<NpcMovement>();
        if (_anim == null) _anim = GetComponent<NpcAnimatorController>();

        _movement.Initialize(_anim);
    }

    public void Initialize(NpcDataSO data)
    {
        _npcData = data;
        GenerateTimeOffset();
    }

    private void Start()
    {
        NpcScheduleManager.Instance.Register(this);
        _isInteracting = false;
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

    // 플레이어가 Npc와 상호작용을 시작할 때, Npc의 이동을 멈추고 플레이어를 바라보도록 합니다.
    public bool CanStartInteraction(Transform interactor)
    {
        if (_isInteracting)
        {
            return false;
        }

        return true;
    }

    public async void StartInteraction(Transform interactor)
    {
        _isInteracting = true;
        _currentInteractor = interactor;

        _movement.Stop();

        if (interactor != null)
        {
            await _movement.FaceTargetAsync(interactor.position);
        }
        _anim?.PlayGreet();
    }

    public void EndInteraction()
    {
        _isInteracting = false;
        _currentInteractor = null;

        // todo. 추후 TimeManager로 바꾸기
        if (TestTimeManager.Instance != null)
        {
            ResumeScheduleByCurrentTime(TestTimeManager.Instance.CurrentTime);
        }
    }

    // 시간이 되면 Npc가 다음 일정대로 움직이는 것을 시도합니다.
    public bool TryGetNextScheduleEntry(int time, out NpcScheduleEntry entry)
    {
        entry = null;

        if (_npcSchedule == null || _npcSchedule.ScheduleEntries == null) return false;
        if (_currentScheduleIndex >= _npcSchedule.ScheduleEntries.Count) return false;
        if (_isInteracting) return false;

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
        if (_npcData == null || _isInteracting) return;

        bool found = NpcLocationManager.Instance.TryGetLocation(
            _npcData.NpcId,
            entry.NpcLocationType,
            entry.LocationKey,
            out Vector3 targetPosition);
        if (!found)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{_npcData.NpcName}의 목적지를 찾지 못했습니다.");
#endif
            return;
        }

        _movement.MoveTo(targetPosition);

#if UNITY_EDITOR
        Debug.Log($"{_npcData.NpcName}가 이동합니다: {entry.NpcLocationType} / {entry.LocationKey}");
#endif
    }

    // 하루가 지나면 스케줄을 리셋합니다.
    public void ResetScheduleForNewDay()
    {
        _isInteracting = false;
        _currentScheduleIndex = 0;
        GenerateTimeOffset();

        if (_npcSchedule == null || _npcSchedule.ScheduleEntries == null || _npcSchedule.ScheduleEntries.Count == 0) return;

        NpcScheduleEntry firstEntry = _npcSchedule.ScheduleEntries[0];
        bool found = NpcLocationManager.Instance.TryGetLocation(
            _npcData.NpcId,
            firstEntry.NpcLocationType,
            firstEntry.LocationKey,
            out Vector3 startPosition);

        if (!found)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{_npcData.NpcName}의 하루 시작 위치를 찾지 못했습니다.");
#endif
            return;
        }

        _movement.TeleportTo(startPosition);
    }

    // 대화 등으로 스케줄이 끊기면 재개합니다.
    public void ResumeScheduleByCurrentTime(int currentTime)
    {
        if (_npcSchedule == null || _npcSchedule.ScheduleEntries == null || _npcSchedule.ScheduleEntries.Count == 0) return;

        NpcScheduleEntry latestValidEntry = null;
        int latestIndex = _currentScheduleIndex;

        for (int i = 0; i < _npcSchedule.ScheduleEntries.Count; i++)
        {
            var entry = _npcSchedule.ScheduleEntries[i];
            int scheduleTime = entry.ScheduleTime + _timeOffset;

            if (currentTime >= scheduleTime)
            {
                latestValidEntry = entry;
                latestIndex = i + 1;
            }
            else
            {
                break;
            }
        }

        if (latestValidEntry != null)
        {
            _currentScheduleIndex = latestIndex;
            ExecuteSchedule(latestValidEntry);
        }
    }
}
