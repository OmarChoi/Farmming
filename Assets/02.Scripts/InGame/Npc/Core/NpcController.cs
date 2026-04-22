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
    [SerializeField] private NpcVoicePlayer _voice;

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
    public NpcVoicePlayer Voice => _voice;
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
        if (_voice == null) _voice = GetComponent<NpcVoicePlayer>();
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
        TryRegisterWithBuilding();
    }

    /// 마스터가 PhotonNetwork.Instantiate 시 건물의 ViewID를 InstantiationData[0]로 실어 보낸다.
    /// 양쪽 클라이언트 모두 이걸 읽어 BaseBuilding.SetNpc(this)를 호출 — 비마스터가
    /// 건물-NPC 바인딩을 유지할 수 있게 해 주는 유일 경로. 마스터도 호출하지만 SetNpc가 멱등.
    private void TryRegisterWithBuilding()
    {
        if (PhotonView == null) return;
        object[] data = PhotonView.InstantiationData;
        if (data == null || data.Length == 0) return;
        if (data[0] is not int buildingViewId || buildingViewId == 0) return;

        PhotonView buildingView = PhotonView.Find(buildingViewId);
        if (buildingView == null) return;

        BaseBuilding building = buildingView.GetComponent<BaseBuilding>();
        if (building != null)
            building.SetNpc(this);
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
            if (_currentInteractor == player?.transform) return true; // 같은 플레이어의 중복 요청 허용 여부는 선택
            return false;
        }

        if (!IsInteractionAvailableNow())
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

        if (_npcSchedule != null && _currentScheduleIndex < _npcSchedule.ScheduleEntries.Count)
        {
            ResumeScheduleByCurrentTime(TimeEvents.CurrentTime);
        }
    }

    [PunRPC]
    private void RPC_EndInteraction()
    {
        _isInteracting = false;
        _currentInteractor = null;

        if (_npcSchedule != null && _currentScheduleIndex < _npcSchedule.ScheduleEntries.Count)
        {
            ResumeScheduleByCurrentTime(TimeEvents.CurrentTime);
        }
    }

    // 필요 시 강제로 대화를 종료합니다.
    public void ForceEndInteraction()
    {
        _isInteracting = false;
        _currentInteractor = null;

        if (!IsMine && !IsLocalOnly)
        {
            PhotonView.RPC(nameof(RPC_ForceEndInteraction), RpcTarget.MasterClient);
            return;
        }

        ResumeScheduleAfterForceEnd();
    }

    [PunRPC]
    private void RPC_ForceEndInteraction()
    {
        _isInteracting = false;
        _currentInteractor = null;
        ResumeScheduleAfterForceEnd();
    }

    private void ResumeScheduleAfterForceEnd()
    {
        if (_npcSchedule != null && _currentScheduleIndex < _npcSchedule.ScheduleEntries.Count)
        {
            ResumeScheduleByCurrentTime(TimeEvents.CurrentTime);
        }
    }

    // 시간이 되면 Npc가 다음 일정대로 움직이는 것을 시도합니다.
    public bool TryGetNextScheduleEntry(GameTime time, out NpcScheduleEntry entry)
    {
        entry = null;

        if (_npcSchedule == null || _npcSchedule.ScheduleEntries == null) return false;
        if (_currentScheduleIndex >= _npcSchedule.ScheduleEntries.Count) return false;
        if (_isInteracting) return false;

        NpcScheduleEntry nextEntry = _npcSchedule.ScheduleEntries[_currentScheduleIndex];
        int scheduleMinutes = nextEntry.ScheduleTime.TotalMinutes + _timeOffset;

        if (time.TotalMinutes >= scheduleMinutes)
        {
            entry = nextEntry;
            return true;
        }

        return false;
    }

    // 스케줄 인덱스를 한 칸 전진합니다. NpcScheduleManager에서 ExecuteSchedule 성공 후 호출합니다.
    public void AdvanceScheduleIndex()
    {
        _currentScheduleIndex++;
    }

    // 일정이 있다면 스케줄대로 행동을 실행합니다. 인덱스는 건드리지 않습니다.
    public bool ExecuteSchedule(NpcScheduleEntry entry)
    {
        if (_npcData == null || _isInteracting) return false;

        if (!TryGetScheduleTargetPosition(entry, out Vector3 targetPosition))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{_npcData.NpcName}의 목적지를 찾지 못했습니다. ({entry.NpcLocationType} / {entry.LocationKey})");
#endif
            return false;
        }

        _movement.MoveTo(targetPosition, 0f);
        _wanderBasePosition = targetPosition;

#if UNITY_EDITOR
        Debug.Log($"{_npcData.NpcName}가 이동합니다: {entry.NpcLocationType} / {entry.LocationKey} -> {targetPosition}");
#endif
        return true;
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

        // _wanderBasePosition 탐색 실패 시 현재 실제 위치 기준으로 재시도합니다. (예: 외부 요인으로 위치가 밀렸을 때)
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit fallbackHit, entry.WanderingRadius, NavMesh.AllAreas))
        {
            _wanderBasePosition = fallbackHit.position;  // 기준점도 보정합니다.
            targetPosition = fallbackHit.position;
            return true;
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

        // Wandering을 제외한 첫 번째 항목을 찾아 하루 시작 위치로 사용합니다.
        for (int i = 0; i < _npcSchedule.ScheduleEntries.Count; i++)
        {
            NpcScheduleEntry candidate = _npcSchedule.ScheduleEntries[i];
            if (candidate.NpcLocationType == ENpcLocationType.Wandering) continue;

            if (NpcLocationManager.Instance.TryGetLocation(
                RuntimeNpcKey,
                candidate.NpcLocationType,
                candidate.LocationKey,
                out Vector3 startPosition))
            {
                _wanderBasePosition = startPosition;
                _movement.TeleportTo(startPosition);
                return;
            }
        }

#if UNITY_EDITOR
        Debug.LogWarning($"{_npcData?.NpcName ?? "Unknown NPC"}의 하루 시작 위치를 찾지 못했습니다. 현재 위치를 유지합니다.");
#endif
    }

    public void SetInitialScheduleBase(Vector3 basePosition)
    {
        _wanderBasePosition = basePosition;
    }

    public void SyncScheduleToCurrentTime()
    {
        ResumeScheduleByCurrentTime(TimeEvents.CurrentTime);
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
            int scheduleMinutes = entry.ScheduleTime.TotalMinutes + _timeOffset;

            if (currentTime.TotalMinutes >= scheduleMinutes)
            {
                latestValidEntry = entry;
                latestIndex = i + 1;
            }
            else
            {
                break;
            }
        }

        if (latestValidEntry == null) return;

        // Index가 Count를 넘지 않도록 클램프합니다. 넘는 경우는 현재 시간이 마지막 일정 이후인 경우입니다.
        _currentScheduleIndex = Mathf.Min(latestIndex, _npcSchedule.ScheduleEntries.Count);

        if (!TryGetScheduleTargetPosition(latestValidEntry, out Vector3 targetPosition))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{_npcData.NpcName} 재개 목적지 없음: {latestValidEntry.NpcLocationType} / {latestValidEntry.LocationKey}");
#endif
            return;
        }

        _movement.MoveTo(targetPosition, 0f);
        _wanderBasePosition = targetPosition;

#if UNITY_EDITOR
        Debug.Log($"{_npcData.NpcName} 스케줄 재개: {latestValidEntry.NpcLocationType} / {latestValidEntry.LocationKey}");
#endif
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

    // 상호작용이 가능한 지 판단하는 메서드입니다.
    public bool IsInteractionAvailableNow()
    {
        if (_npcData == null) return false;

        NpcScheduleEntry currentEntry = GetCurrentScheduleEntry(TimeEvents.CurrentTime);
        if (currentEntry != null)
        {
            switch (currentEntry.InteractionRule)
            {
                case ENpcInteractionRule.Allow:
                    return true;

                case ENpcInteractionRule.Block:
                    return false;

                case ENpcInteractionRule.UseDefault:
                    break;
            }
        }

        return IsWithinDefaultInteractionTime(TimeEvents.CurrentTime);
    }

    private NpcScheduleEntry GetCurrentScheduleEntry(GameTime currentTime)
    {
        if (_npcSchedule == null || _npcSchedule.ScheduleEntries == null || _npcSchedule.ScheduleEntries.Count == 0) return null;

        NpcScheduleEntry currentEntry = null;

        for (int i = 0; i < _npcSchedule.ScheduleEntries.Count; i++)
        {
            NpcScheduleEntry entry = _npcSchedule.ScheduleEntries[i];
            int scheduleMinutes = entry.ScheduleTime.TotalMinutes + _timeOffset;

            if (currentTime.TotalMinutes >= scheduleMinutes)
            {
                currentEntry = entry;
            }
            else
            {
                break;
            }
        }

        return currentEntry;
    }

    private bool IsWithinDefaultInteractionTime(GameTime currentTime)
    {
        if (_npcData == null || !_npcData.UseInteractionTimeRange) return true;

        int currentMinutes = currentTime.TotalMinutes;
        int startMinutes = _npcData.InteractionStartTime.TotalMinutes;
        int endMinutes = _npcData.InteractionEndTime.TotalMinutes;

        // 시작과 끝이 같으면 하루 종일 허용으로 봅니다.
        if (startMinutes == endMinutes) return true;

        // 일반 구간: 예) 08:00 ~ 22:00
        if (startMinutes < endMinutes)
        {
            return currentMinutes >= startMinutes && currentMinutes < endMinutes;
        }

        // 자정 넘김 구간: 예) 20:00 ~ 06:00
        return currentMinutes >= startMinutes || currentMinutes < endMinutes;
    }
}
