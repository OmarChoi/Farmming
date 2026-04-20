using UnityEngine;

public class ShrineQuestMarkView : MonoBehaviour
{
    [Header("참조 컴포넌트")]
    [SerializeField] private WorldMarkView _worldMarkView;
    [SerializeField] private QuestMarkService _questMarkService;

    private void Awake()
    {
        if (_worldMarkView == null)
        {
            _worldMarkView = GetComponentInChildren<WorldMarkView>(true);
        }

        if (_questMarkService == null)
        {
            _questMarkService = FindFirstObjectByType<QuestMarkService>();
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

        QuestManager.OnQuestDataLoaded += Refresh;
        TimeEvents.OnNetDayChanged += Refresh;

        if (_questMarkService != null)
        {
            _questMarkService.OnMarkStateChanged += Refresh;
        }

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

        QuestManager.OnQuestDataLoaded -= Refresh;
        TimeEvents.OnNetDayChanged -= Refresh;

        if (_questMarkService != null)
        {
            _questMarkService.OnMarkStateChanged -= Refresh;
        }
    }

    private void OnQuestChanged(QuestRuntimeData _)
    {
        Refresh();
    }

    private void OnQuestRemoved(string _)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (_questMarkService == null || _worldMarkView == null)
        {
            _worldMarkView?.HideAll();
            return;
        }

        if (QuestManager.Instance == null || !QuestManager.Instance.IsLoaded)
        {
            _worldMarkView.HideAll();
            return;
        }

        EWorldMarkVisualType visualType = _questMarkService.GetShrineQuestMarkVisualType();

        switch (visualType)
        {
            case EWorldMarkVisualType.Updated:
                _worldMarkView.ShowUpdated();
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
