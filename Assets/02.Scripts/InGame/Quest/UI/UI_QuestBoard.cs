using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class UI_QuestBoard : MonoBehaviour
{
    [Header("컴포넌트 옵션")]
    [SerializeField] private Transform _slotParent;
    [SerializeField] private UI_QuestBoardSlot _slotPrefab;
    [SerializeField] private GameObject _questBoardRoot;

    [Header("닫기 버튼")]
    [SerializeField] private Button _exitButton;

    private readonly List<UI_QuestBoardSlot> _slots = new();

    private List<QuestDataSO> _currentQuests = new();

    public Action OnCloseRequested;

    private void Awake()
    {
        if (_exitButton != null)
        {
            _exitButton.onClick.AddListener(OnClickCloseButton);
        }
    }

    private void Start()
    {
        _questBoardRoot.SetActive(false);
    }

    public void Open(List<QuestDataSO> quests)
    {
        _currentQuests.Clear();

        if (quests != null)
        {
            _currentQuests.AddRange(quests);
        }

        _questBoardRoot.SetActive(true);
        CreateOrRefreshSlots();
    }

    public void Close()
    {
        _questBoardRoot.SetActive(false);
        _currentQuests.Clear();
    }

    private void CreateOrRefreshSlots()
    {
        if (_currentQuests == null) return;

        int slotCount = _currentQuests.Count;

        while (_slots.Count < slotCount)
        {
            UI_QuestBoardSlot newSlot = Instantiate(_slotPrefab, _slotParent);
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

    public void OnQuestSlotClicked(QuestDataSO questData)
    {
        if (questData == null) return;
        if (QuestManager.Instance == null) return;
        if (string.IsNullOrEmpty(questData.QuestId)) return;

        QuestManager questManager = QuestManager.Instance;
        string questId = questData.QuestId;

        bool hasQuest = questManager.HasQuest(questId);

        if (hasQuest)
        {
            if (questManager.CanCompleteQuest(questId))
            {
                bool success = questManager.CompleteQuest(questId);

#if UNITY_EDITOR
                if (success)
                {
                    Debug.Log($"퀘스트 완료: {questData.QuestName}");
                }
                else
                {
                    Debug.LogWarning($"퀘스트 완료 실패: {questData.QuestName}");
                }
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log($"이미 진행 중인 퀘스트입니다: {questData.QuestName}");
#endif
            }
        }
        else
        {
            if (questManager.CanAcceptQuest(questData))
            {
                bool success = questManager.AcceptQuest(questData);

#if UNITY_EDITOR
                if (success)
                {
                    Debug.Log($"퀘스트 수락: {questData.QuestName}");
                }
                else
                {
                    Debug.LogWarning($"퀘스트 수락 실패: {questData.QuestName}");
                }
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log($"현재 수락할 수 없는 퀘스트입니다: {questData.QuestName}");
#endif
            }
        }

        CreateOrRefreshSlots();
    }

    public void OnClickCloseButton()
    {
        OnCloseRequested?.Invoke();
    }
}
