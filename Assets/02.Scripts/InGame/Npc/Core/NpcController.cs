using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NpcController : MonoBehaviour
{
    public PhotonView PhotonView { get; private set; }
    public bool IsMine => PhotonView == null || !PhotonNetwork.IsConnected || PhotonView.IsMine;
    public bool IsLocalOnly { get; private set; }

    [Header("Npc 컴포넌트")]
    [SerializeField] private Animator _animator;
    [SerializeField] private NpcMovement _movement;
    [SerializeField] private NpcAnimatorController _anim;

    [Header("Npc 데이터")]
    [SerializeField] private NpcScheduleSO _npcSchedule;
    [SerializeField] private NpcInteractionOption[] _interactionOptions;

    [Header("런타임 식별자")]
    [SerializeField] private string _runtimeNpcKey;

    [Header("상점 옵션")]
    [SerializeField] private Shop _shop;

    [Header("퀘스트 옵션")]
    [SerializeField] private NpcDataSO _npcData;
    [SerializeField] private NpcQuest _npcQuest;

    private Transform _currentInteractor;

    private int _timeOffset;
    private int _currentScheduleIndex = 0;

    private bool _isInteracting;

    private Vector3 _wanderBasePosition;

    private readonly List<NpcInteractionOption> _runtimeInteractionOptions = new();
    private NpcInteractionOption[] _interactionOptionCache;

    public Animator Animator => _animator;
    public NpcAnimatorController Anim => _anim;
    public NpcDataSO Data => _npcData;
    public NpcQuest Quest => _npcQuest;
    public NpcScheduleSO Schedule => _npcSchedule;
    public Shop Shop => _shop;
    public Transform CurrentInteractor => _currentInteractor;
    public NpcInteractionOption[] InteractionOptions => GetInteractionOptions();
    public bool IsInteracting => _isInteracting;
    public bool AutoStartQuestOnInteract => _npcData != null && _npcData.AutoStartQuestOnInteract;
    public string BaseNpcId => _npcData != null ? _npcData.NpcId : string.Empty;

    public string RuntimeNpcKey
    {
        get
        {
            if (!string.IsNullOrEmpty(_runtimeNpcKey)) return _runtimeNpcKey;

            return BaseNpcId;
        }
    }

    private void Awake()
    {
        PhotonView = GetComponent<PhotonView>();
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_movement == null) _movement = GetComponent<NpcMovement>();
        if (_anim == null) _anim = GetComponent<NpcAnimatorController>();
        if (_npcQuest == null) _npcQuest = GetComponent<NpcQuest>();
    }

    public void Initialize(NpcDataSO data, bool isLocalOnly = false, string runtimeNpcKey = null)
    {
        _npcData = data;
        IsLocalOnly = isLocalOnly;
        GenerateTimeOffset();
        _runtimeNpcKey = string.IsNullOrEmpty(runtimeNpcKey) && data != null ? data.NpcId : runtimeNpcKey;

        IMovementAnimator movementAnimator = _anim;

        if (_movement != null && _npcData != null)
        {
            _movement.SetOwner(IsMine);
            _movement.Initialize(
                movementAnimator,
                _npcData.WalkSpeed,
                _npcData.RunSpeed,
                _npcData.JumpDuration,
                _npcData.JumpHeight);
        }
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
    public bool CanStartInteraction(PlayerController player)
    {
        if (_isInteracting)
        {
            return false;
        }

        return true;
    }

    public void StartInteraction(PlayerController player)
    {
        _isInteracting = true;
        _currentInteractor = player.transform;

        if (!IsMine && !IsLocalOnly)
        {
            // 클라이언트: 마스터에게 상호작용 요청 RPC 전송
            Vector3 interactorPos = player != null ? player.transform.position : transform.position;
            PhotonView.RPC(nameof(RPC_StartInteraction), RpcTarget.MasterClient, interactorPos);
            return;
        }

        // 마스터 또는 오프라인: 직접 실행
        Vector3 targetPos = player != null ? player.transform.position : transform.position;
        StartInteractionAsync(targetPos).Forget();
    }

    [PunRPC]
    private void RPC_StartInteraction(Vector3 interactorPosition)
    {
        _isInteracting = true;
        StartInteractionAsync(interactorPosition).Forget();
    }

    private async UniTaskVoid StartInteractionAsync(Vector3 interactorPosition)
    {
        _movement.Stop();
        await _movement.FaceTargetAsync(interactorPosition)
            .AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
        PlayGreetAll();
    }

    private void PlayGreetAll()
    {
        _anim?.PlayGreet();
        if (IsMine && !IsLocalOnly)
        {
            PhotonView.RPC(nameof(RPC_PlayNpcGreet), RpcTarget.Others);
        }
    }

    [PunRPC]
    private void RPC_PlayNpcGreet()
    {
        _anim?.PlayGreet();
    }

    public void EndInteraction()
    {
        _isInteracting = false;
        _currentInteractor = null;

        if (!IsMine && !IsLocalOnly)
        {
            // 클라이언트: 마스터에게 상호작용 종료 RPC 전송
            PhotonView.RPC(nameof(RPC_EndInteraction), RpcTarget.MasterClient);
            return;
        }

        ResumeScheduleByCurrentTime(TimeEvents.CurrentTime);
    }

    [PunRPC]
    private void RPC_EndInteraction()
    {
        _isInteracting = false;
        _currentInteractor = null;
        ResumeScheduleByCurrentTime(TimeEvents.CurrentTime);
    }

    // 시간이 되면 Npc가 다음 일정대로 움직이는 것을 시도합니다.
    public bool TryGetNextScheduleEntry(GameTime time, out NpcScheduleEntry entry)
    {
        entry = null;

        if (_npcSchedule == null || _npcSchedule.ScheduleEntries == null) return false;
        if (_currentScheduleIndex >= _npcSchedule.ScheduleEntries.Count) return false;
        if (_isInteracting) return false;

        NpcScheduleEntry nextEntry = _npcSchedule.ScheduleEntries[_currentScheduleIndex];
        GameTime scheduleTime = nextEntry.ScheduleTime + _timeOffset;

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

        if (!TryGetScheduleTargetPosition(entry, out Vector3 targetPosition))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{_npcData.NpcName}의 목적지를 찾지 못했습니다. ({entry.NpcLocationType} / {entry.LocationKey})");
#endif
            return;
        }
        _movement.MoveTo(targetPosition, 0f);

        if (entry.NpcLocationType != ENpcLocationType.Wandering)
        {
            _wanderBasePosition = targetPosition;
        }

#if UNITY_EDITOR
        Debug.Log($"{_npcData.NpcName}가 이동합니다: {entry.NpcLocationType} / {entry.LocationKey}");
#endif
    }

    private bool TryGetScheduleTargetPosition(NpcScheduleEntry entry, out Vector3 targetPosition)
    {
        targetPosition = Vector3.zero;

        if (entry.NpcLocationType == ENpcLocationType.Wandering)
        {
            if (entry.WanderSearchStep <= 0f)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"WanderSearchStep은 양수여야 합니다. {_npcData.NpcId}.");
#endif
                return false;
            }
            return TryGetWanderPosition(entry, out targetPosition);
        }
        Debug.Log($"[NpcController.TryGetScheduleTargetPosition] npc={_npcData?.NpcName}, runtimeKey={RuntimeNpcKey}, type={entry.NpcLocationType}, locationKey={entry.LocationKey}");
        return NpcLocationManager.Instance.TryGetLocation(
            RuntimeNpcKey,
            entry.NpcLocationType,
            entry.LocationKey,
            out targetPosition);
    }

    private bool TryGetWanderPosition(NpcScheduleEntry entry, out Vector3 targetPosition)
    {
        targetPosition = Vector3.zero;

        Vector3 direction = entry.WanderingDirection.sqrMagnitude > 0.01f
            ? entry.WanderingDirection.normalized
            : transform.forward;

        for (float distance = entry.WanderingDistance; distance >= entry.WanderSearchStep; distance -= entry.WanderSearchStep)
        {
            Vector3 candidate = _wanderBasePosition + direction * distance;

            Vector2 rand = Random.insideUnitCircle * entry.WanderingRadius;
            candidate += new Vector3(rand.x, 0f, rand.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, entry.WanderingRadius, NavMesh.AllAreas))
            {
                targetPosition = hit.position;
                return true;
            }
        }

        return false;
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
            RuntimeNpcKey,
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
        _wanderBasePosition = startPosition;
        _movement.TeleportTo(startPosition);
    }

    // 대화 등으로 스케줄이 끊기면 재개합니다.
    public void ResumeScheduleByCurrentTime(GameTime currentTime)
    {
        if (_npcSchedule == null || _npcSchedule.ScheduleEntries == null || _npcSchedule.ScheduleEntries.Count == 0) return;

        NpcScheduleEntry latestValidEntry = null;
        int latestIndex = 0;

        for (int i = 0; i < _npcSchedule.ScheduleEntries.Count; i++)
        {
            var entry = _npcSchedule.ScheduleEntries[i];
            GameTime scheduleTime = entry.ScheduleTime + _timeOffset;

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

            if (latestValidEntry.NpcLocationType == ENpcLocationType.Wandering)
            {
                _wanderBasePosition = transform.position;
            }

            ExecuteSchedule(latestValidEntry);
        }
    }

    private NpcInteractionOption[] GetInteractionOptions()
    {
        if (_interactionOptionCache != null)
        {
            return _interactionOptionCache;
        }

        int serializedCount = _interactionOptions != null ? _interactionOptions.Length : 0;
        int runtimeCount = _runtimeInteractionOptions.Count;
        if (runtimeCount == 0)
        {
            _interactionOptionCache = _interactionOptions ?? System.Array.Empty<NpcInteractionOption>();
            return _interactionOptionCache;
        }

        _interactionOptionCache = new NpcInteractionOption[serializedCount + runtimeCount];
        _interactionOptions?.CopyTo(_interactionOptionCache, 0);
        _runtimeInteractionOptions.CopyTo(_interactionOptionCache, serializedCount);

        return _interactionOptionCache;
    }

    public void SetInitialScheduleBase(Vector3 basePosition)
    {
        _wanderBasePosition = basePosition;
    }

    public void SyncScheduleToCurrentTime()
    {
        ResumeScheduleByCurrentTime(TimeEvents.CurrentTime);
    }
}
