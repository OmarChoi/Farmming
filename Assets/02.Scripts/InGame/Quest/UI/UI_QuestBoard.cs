using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class UI_QuestBoard : MonoBehaviour
{
    [Header("컴포넌트 옵션")]
    [SerializeField] private Transform _slotParent;
    [SerializeField] private UI_QuestBoardSlot _slotPrefab;
    [SerializeField] private GameObject _uiQuestBoardRoot;

    [Header("닫기 버튼")]
    [SerializeField] private Button _exitButton;

    private readonly List<UI_QuestBoardSlot> _slots = new();

    private QuestBoardDataSO _currentQuestBoardData;

    public Action OnCloseRequested;

    public void Open(QuestBoardDataSO questData)
    {
        _currentQuestBoardData = questData;
        _uiQuestBoardRoot.SetActive(true);

        CreateOrRefreshSlots();
    }

    public void Close()
    {
        _uiQuestBoardRoot.SetActive(false);
        _currentQuestBoardData = null;
    }

    private void CreateOrRefreshSlots()
    {
        if (_currentQuestBoardData == null || _currentQuestBoardData.AllQuests == null) return;
        int slotCount = _currentQuestBoardData.AllQuests.Count;

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
                _slots[i].Refresh(_currentQuestBoardData.AllQuests[i]);
            }
        }
    }

    public void OnQuestSlotClicked(QuestDataSO questData)
    {
        if (_currentQuestBoardData == null || questData == null) return;

        bool success = QuestManager.Instance.AcceptQuest(questData);

#if UNITY_EDITOR
        if (success)
        {
            Debug.Log($"퀘스트 수락: {questData.QuestName}");
        }
        else
        {
            Debug.LogWarning($"퀘스트 수락 실패: {questData?.QuestName}");
        }
#endif
    }

    public void OnClickCloseButton()
    {
        OnCloseRequested?.Invoke();
    }
}
