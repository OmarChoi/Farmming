using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class UI_QuestJournal : MonoBehaviour
{
    [Header("컴포넌트 옵션")]
    [SerializeField] private Transform _slotParent;
    [SerializeField] private UI_QuestJournalSlot _slotPrefab;
    [SerializeField] private GameObject _uiQuestJournalRoot;
    [SerializeField] private QuestManager _questManager;

    private IQuestProgressService _questProgressService;

    [Header("팝업 트윈")]
    [SerializeField] private UI_PopupDoTween _popupDoTween;

    private readonly List<UI_QuestJournalSlot> _slots = new();
    private readonly List<QuestRuntimeData> _currentQuests = new();

    private void Awake()
    {
        if (_questManager == null)
        {
            _questManager = FindFirstObjectByType<QuestManager>();
        }
        _questProgressService = _questManager;
    }

    private void OnEnable()
    {
        if (_questProgressService != null)
        {
            _questProgressService.OnQuestAccepted += HandleQuestChanged;
            _questProgressService.OnQuestUpdated += HandleQuestChanged;
            _questProgressService.OnQuestCompleted += HandleQuestChanged;
            _questProgressService.OnQuestRemoved += HandleQuestRemoved;
        }
    }

    private void OnDisable()
    {
        if (_questProgressService != null)
        {
            _questProgressService.OnQuestAccepted -= HandleQuestChanged;
            _questProgressService.OnQuestUpdated -= HandleQuestChanged;
            _questProgressService.OnQuestCompleted -= HandleQuestChanged;
            _questProgressService.OnQuestRemoved -= HandleQuestRemoved;
        }
    }

    public bool IsOpen()
    {
        return _uiQuestJournalRoot != null && _uiQuestJournalRoot.activeSelf;
    }

    public async UniTask OpenAsync()
    {
        RefreshQuestList();
        CreateOrRefreshSlots();
        if (_popupDoTween != null)
        {
            await _popupDoTween.PlayOpenAsync();
        }
        else if (_uiQuestJournalRoot != null)
        {
            _uiQuestJournalRoot.SetActive(true);
        }
    }

    public async UniTask CloseAsync()
    {
        if (_popupDoTween != null)
        {
            await _popupDoTween.PlayCloseAsync();
        }
        else if (_uiQuestJournalRoot != null)
        {
            _uiQuestJournalRoot.SetActive(false);
        }
        _currentQuests.Clear();
    }

    private void RefreshQuestList()
    {
        _currentQuests.Clear();

        if (_questProgressService == null) return;

        List<QuestRuntimeData> activeQuests = _questProgressService.GetActiveQuestList();
        if (activeQuests == null) return;

        _currentQuests.AddRange(activeQuests);

        // 완료 가능한 것을 목록의 맨 위로 올려준다.
        _currentQuests.Sort((a, b) =>
        {
            if (a == null || a.QuestData == null) return 1;
            if (b == null || b.QuestData == null) return -1;

            int statusCompare = b.Status.CompareTo(a.Status);
            if (statusCompare != 0) return statusCompare;

            return a.QuestData.QuestCategory.CompareTo(b.QuestData.QuestCategory);
        });
    }

    private void CreateOrRefreshSlots()
    {
        int slotCount = _currentQuests.Count;

        while (_slots.Count < slotCount)
        {
            UI_QuestJournalSlot newSlot = Instantiate(_slotPrefab, _slotParent);
            newSlot.Init(this, _slots.Count);
            _slots.Add(newSlot);
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            bool isActive = i < slotCount;
            _slots[i].gameObject.SetActive(isActive);

            if (isActive)
            {
                _slots[i].Refresh(_currentQuests[i]);
            }
        }
    }

    private void HandleQuestChanged(QuestRuntimeData quest)
    {
        if (!IsOpen()) return;

        RefreshQuestList();
        CreateOrRefreshSlots();
    }

    private void HandleQuestRemoved(string questId)
    {
        if (!IsOpen()) return;

        RefreshQuestList();
        CreateOrRefreshSlots();
    }
}
