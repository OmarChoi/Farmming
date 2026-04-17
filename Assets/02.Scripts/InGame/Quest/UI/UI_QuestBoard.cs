using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

// 일일 퀘스트 보드 팝업. UIController 경로로만 Open/Close되며,
// 게임플레이 쪽 후처리(커서락/상호작용 종료 등)는 UILifecycleActions.OnClose가 담당한다.
public class UI_QuestBoard : UIBase
{
    [Header("컴포넌트 옵션")]
    [SerializeField] private Transform _slotParent;
    [SerializeField] private UI_QuestBoardSlot _slotPrefab;

    [Header("팝업 트윈")]
    [SerializeField] private UI_PopupDoTween _popupDoTween;

    [Header("닫기 버튼")]
    [SerializeField] private Button _exitButton;

    private readonly List<UI_QuestBoardSlot> _slots = new();
    private readonly List<QuestDataSO> _currentQuests = new();

    private void Awake()
    {
        if (_exitButton != null)
        {
            _exitButton.onClick.AddListener(OnClickCloseButton);
        }
    }

    // UIController.OpenAsync의 OnOpen 콜백에서 데이터 주입.
    public void SetQuests(IReadOnlyList<QuestDataSO> quests)
    {
        _currentQuests.Clear();

        if (quests != null)
        {
            _currentQuests.AddRange(quests);
        }
    }

    protected override void OnOpen()
    {
        CreateOrRefreshSlots();
    }

    protected override async UniTask OnOpenAnimation()
    {
        if (_popupDoTween != null)
        {
            await _popupDoTween.PlayOpenAsync();
        }
    }

    protected override async UniTask OnCloseAnimation()
    {
        if (_popupDoTween != null)
        {
            await _popupDoTween.PlayCloseAsync();
        }
    }

    protected override void OnClose()
    {
        _currentQuests.Clear();
    }

    // 현재 퀘스트 목록에 맞춰 슬롯을 재사용/생성.
    private void CreateOrRefreshSlots()
    {
        int slotCount = _currentQuests.Count;

        while (_slots.Count < slotCount)
        {
            UI_QuestBoardSlot newSlot = Instantiate(_slotPrefab, _slotParent);
            newSlot.Initialized(QuestManager.Instance);
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

    // 슬롯에서 수락/완료 버튼을 눌렀을 때 호출.
    public void OnQuestSlotClicked(QuestDataSO questData)
    {
        var questProgressService = QuestManager.Instance;
        if (questData == null || questProgressService == null) return;
        if (string.IsNullOrEmpty(questData.QuestId)) return;

        string questId = questData.QuestId;

        bool hasQuest = questProgressService.HasQuest(questId);

        if (hasQuest)
        {
            if (questProgressService.CanCompleteQuest(questId))
            {
                bool success = questProgressService.CompleteQuest(questId);

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
            if (questProgressService.CanAcceptQuest(questData))
            {
                bool success = questProgressService.AcceptQuest(questData);

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

    // 닫기 버튼 → UIController 경유로 닫아 스택/애니메이션·콜백 처리를 일관 유지.
    private void OnClickCloseButton()
    {
        if (UIController.Instance == null) return;
        UIController.Instance.CloseAsync<UI_QuestBoard>().Forget();
    }
}
