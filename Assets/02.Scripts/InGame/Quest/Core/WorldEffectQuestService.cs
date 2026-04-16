using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

public class WorldEffectQuestService : MonoBehaviour
{
    private const string QuestBuilding = "Shrine";
    public static WorldEffectQuestService Instance { get; private set; }

    [SerializeField] private WorldEffectQuestConfigSO _config;

    private readonly ForcedTimedQuestSaveData _state = new();
    private WorldEffectQuestRuleSO _questInfo;
    private WorldEffectQuestNetworkSync _sync;

    private bool IsMaster => !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
    private bool CanUseNetworkSync => PhotonNetwork.IsConnected && PhotonNetwork.InRoom && _sync != null;

    public string ActiveQuestId => _state.ActiveQuestId;
    public bool HasActiveQuest => !string.IsNullOrEmpty(_state.ActiveQuestId);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        TimeEvents.OnNetDayChanged += OnNetDayChanged;
    }

    private void OnDisable()
    {
        TimeEvents.OnNetDayChanged -= OnNetDayChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RegisterSync(WorldEffectQuestNetworkSync sync)
    {
        _sync = sync;
        if (IsMaster && CanUseNetworkSync)
        {
            _sync.BroadcastSnapshot(ExportSaveData());
        }
    }

    public void UnregisterSync(WorldEffectQuestNetworkSync sync)
    {
        if (_sync == sync) _sync = null;
    }

    public ForcedTimedQuestSaveData ExportSaveData()
    {
        return new ForcedTimedQuestSaveData
        {
            ActiveQuestId = _state.ActiveQuestId,
            AcceptedDay = _state.AcceptedDay,
            ExpireDay = _state.ExpireDay,
            LastTriggerDay = _state.LastTriggerDay,
        };
    }

    public void ImportSaveData(ForcedTimedQuestSaveData data)
    {
        if (data == null)
        {
            ClearStateFields();
        }
        else
        {
            _state.ActiveQuestId = data.ActiveQuestId;
            _state.AcceptedDay = data.AcceptedDay;
            _state.ExpireDay = data.ExpireDay;
            _state.LastTriggerDay = data.LastTriggerDay;
        }
        ApplyStateToJournal();
    }

    public void ApplySnapshot(ForcedTimedQuestSaveData data)
    {
        if (IsMaster) return;
        ImportSaveData(data);
    }

    private void OnNetDayChanged()
    {
        if (!IsMaster || _config == null) return;

        int currentDay = TimeEvents.CurrentDay;

        if (!string.IsNullOrEmpty(_state.ActiveQuestId) && currentDay >= _state.ExpireDay)
        {
            ApplyExpireEffect(_state.ActiveQuestId);
            ClearActive();
        }

        if (ShouldTrigger(currentDay))
        {
            TryIssueNewQuest(currentDay);
        }

        BroadcastSnapshotIfConnected();
        // SaveWorld();
    }

    private bool ShouldTrigger(int currentDay)
    {
        if (_config == null) return false;
        if (!string.IsNullOrEmpty(_state.ActiveQuestId)) return false;
        if (currentDay < _config.FirstTriggerDay) return false;
        if (_state.LastTriggerDay <= 0) return true;
        return currentDay - _state.LastTriggerDay >= _config.QuestIntervalDays;
    }

    private void TryIssueNewQuest(int currentDay)
    {
        var rules = _config.Rules;
        if (rules == null || rules.Count == 0) return;

        WorldEffectQuestRuleSO rule = rules[Random.Range(0, rules.Count)];
        if (rule == null || rule.Quest == null) return;

        _state.ActiveQuestId = rule.Quest.QuestId;
        _state.AcceptedDay = currentDay;
        _state.ExpireDay = currentDay + rule.DurationDays;
        _state.LastTriggerDay = currentDay;
    }

    private void ApplyExpireEffect(string questId)
    {
        WorldEffectQuestRuleSO rule = FindRuleByQuestId(questId);
        if (rule == null || rule.FailureEffect == null) return;
        
        WorldEffectManager.Instance?.AddEffect(
            rule.FailureEffect.EffectId,
            rule.FailureEffect.Kind,
            _config.EffectDurationDays);
    }

    private void ApplySuccessEffect(string questId)
    {
        WorldEffectQuestRuleSO rule = FindRuleByQuestId(questId);
        if (rule == null || rule.SuccessEffect == null) return;

        WorldEffectManager.Instance?.AddEffect(
            rule.SuccessEffect.EffectId,
            rule.SuccessEffect.Kind,
            _config.EffectDurationDays);
    }

    public WorldEffectQuestRuleSO FindRuleByQuestId(string questId)
    {
        if (_config == null || string.IsNullOrEmpty(questId)) return null;

        foreach (var rule in _config.Rules)
        {
            if (rule == null || rule.Quest == null) continue;
            if (rule.Quest.QuestId == questId) return rule;
        }
        return null;
    }

    public void RequestCompleteAtShrine(string questId)
    {
        // 요청자 측에서 진행도 확인 + 아이템 소모 + 퀘스트 완료 처리까지 수행합니다.
        // 서버 권위 검증이 없으므로 요청자를 신뢰합니다.
        if (QuestManager.Instance == null) return;
        if (!QuestManager.Instance.CompleteForcedTimedQuest(questId)) return;

        if (IsMaster)
        {
            HandleCompleteAtShrine(questId);
            return;
        }
        if (CanUseNetworkSync) _sync.RequestCompleteAtShrine(questId);
    }

    public void ExecuteCompleteAtShrine(string questId)
    {
        if (!IsMaster) return;
        HandleCompleteAtShrine(questId);
    }

    private void HandleCompleteAtShrine(string questId)
    {
        if (_config == null) return;
        if (string.IsNullOrEmpty(_state.ActiveQuestId) || _state.ActiveQuestId != questId) return;
        if (TimeEvents.CurrentDay >= _state.ExpireDay) return;
        if (_config.CompletionBuildingId != QuestBuilding) return;

        ApplySuccessEffect(questId);
        ClearActive();

        BroadcastSnapshotIfConnected();
        // SaveWorld();
    }

    private void BroadcastSnapshotIfConnected()
    {
        ApplyStateToJournal();
        if (CanUseNetworkSync) _sync.BroadcastSnapshot(ExportSaveData());
    }

    public void ApplyStateToJournal()
    {
        if (QuestManager.Instance == null) return;

        if (string.IsNullOrEmpty(_state.ActiveQuestId))
        {
            if (_config != null)
            {
                foreach (var rule in _config.Rules)
                {
                    if (rule == null || rule.Quest == null) continue;
                    QuestManager.Instance.RemoveQuest(rule.Quest.QuestId);
                }
            }
            return;
        }

        WorldEffectQuestRuleSO active = FindRuleByQuestId(_state.ActiveQuestId);
        if (active == null || active.Quest == null) return;

        QuestManager.Instance.AddOrUpdateForcedTimedQuest(active.Quest, _state.AcceptedDay, _state.ExpireDay);
    }

    private void ClearActive()
    {
        if (!string.IsNullOrEmpty(_state.ActiveQuestId))
        {
            string questID = _state.ActiveQuestId;
            _state.ActiveQuestId = string.Empty;
            QuestManager.Instance?.RemoveQuest(questID);
        }
        _state.AcceptedDay = 0;
        _state.ExpireDay = 0;
    }

    private void ClearStateFields()
    {
        _state.ActiveQuestId = null;
        _state.AcceptedDay = 0;
        _state.ExpireDay = 0;
        _state.LastTriggerDay = 0;
    }

    private void SaveWorld()
    {
        if (!IsMaster) return;
        int slot = RoomManager.Instance != null ? RoomManager.Instance.SelectedSlot : 0;
        SaveManager.Instance?.SaveAsync(slot).Forget();
    }
}
