using UnityEngine;

public class NpcQuestMarkView : MonoBehaviour
{
    [Header("참조 컴포넌트")]
    [SerializeField] private NpcController _npc;
    [SerializeField] private WorldMarkView _worldMarkView;

    [SerializeField] private QuestMarkService _questMarkService;

    private void Awake()
    {
        if (_npc == null)
        {
            _npc = GetComponentInParent<NpcController>();
        }

        if (_worldMarkView == null)
        {
            _worldMarkView = GetComponentInChildren<WorldMarkView>(true);
        }

        if (_questMarkService == null)
        {
            _questMarkService = FindFirstObjectByType<QuestMarkService>();
        }

        if (NpcFriendshipManager.Instance != null)
        {
            NpcFriendshipManager.Instance.OnFriendshipChanged += OnFriendshipChanged;
        }
    }

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted += OnQuestChanged;
            QuestManager.Instance.OnQuestUpdated += OnQuestChanged;
            QuestManager.Instance.OnQuestCompleted += OnQuestChanged;
            QuestManager.Instance.OnQuestRemoved += OnQuestRemoved;
        }

        if (NpcFriendshipManager.Instance != null)
        {
            NpcFriendshipManager.Instance.OnFriendshipChanged += OnFriendshipChanged;
        }

        QuestManager.OnQuestDataLoaded += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted -= OnQuestChanged;
            QuestManager.Instance.OnQuestUpdated -= OnQuestChanged;
            QuestManager.Instance.OnQuestCompleted -= OnQuestChanged;
            QuestManager.Instance.OnQuestRemoved -= OnQuestRemoved;
        }
        
        if (NpcFriendshipManager.Instance != null)
        {
            NpcFriendshipManager.Instance.OnFriendshipChanged -= OnFriendshipChanged;
        }

        QuestManager.OnQuestDataLoaded -= Refresh;
    }

    private void OnQuestChanged(QuestRuntimeData _)
    {
        Refresh();
    }

    private void OnQuestRemoved(string _)
    {
        Refresh();
    }

    private void OnFriendshipChanged(string npcId, int oldValue, int newValue, ENpcFriendshipReason reason)
    {
        if (_npc == null || _npc.Data == null || _npc.Data.NpcId != npcId) return;

        Refresh();
    }

    public void Refresh()
    {
        if (_npc == null || _questMarkService == null || _worldMarkView == null)
        {
            _worldMarkView?.HideAll();
            return;
        }

        if (QuestManager.Instance == null || !QuestManager.Instance.IsLoaded)
        {
            _worldMarkView.HideAll();
            return;
        }

        NpcQuest provider = _npc.GetComponent<NpcQuest>();
        if (provider == null)
        {
            _worldMarkView.HideAll();
            return;
        }

        EWorldMarkVisualType visualType = _questMarkService.GetNpcQuestMarkVisualType(_npc);

        switch (visualType)
        {
            case EWorldMarkVisualType.Available:
                _worldMarkView.ShowAvailable();
                break;

            case EWorldMarkVisualType.InProgress:
                _worldMarkView.ShowInProgress();
                break;

            case EWorldMarkVisualType.CanComplete:
                _worldMarkView.ShowCanComplete();
                break;

            default:
                _worldMarkView.HideAll();
                break;
        }
    }
}
