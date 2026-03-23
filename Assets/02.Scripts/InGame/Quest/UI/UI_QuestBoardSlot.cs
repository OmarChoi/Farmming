using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_QuestBoardSlot : MonoBehaviour
{
    [Header("퀘스트 슬롯 텍스트")]
    [SerializeField] private TextMeshProUGUI _questNameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _questRewardText;

    [Header("퀘스트 수락 버튼")]
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Image _acceptButtonImage;
    [SerializeField] private TextMeshProUGUI _acceptButtonText;

    private UI_QuestBoard _uiQuestBoard;
    private int _slotIndex;
    private QuestDataSO _questData;

    public int SlotIndex => _slotIndex;
    public QuestDataSO QuestData => _questData;

    public void Init(UI_QuestBoard uiQuestBoard, int index)
    {
        _uiQuestBoard = uiQuestBoard;
        _slotIndex = index;

        if (_acceptButton != null)
        {
            _acceptButton.onClick.RemoveAllListeners();
            _acceptButton.onClick.AddListener(OnClickAcceptButton);
        }
    }

    public void Refresh(QuestDataSO quest)
    {
        _questData = quest;

        if (quest == null)
        {
            _questNameText.text = "";
            _descriptionText.text = "";
            _questRewardText.text = "";

            if (_acceptButton != null)
            {
                _acceptButton.interactable = false;
            }
            if (_acceptButtonText != null)
            {
                _acceptButtonText.text = "";
            }
            return;
        }

        _questNameText.text = quest.QuestName;
        _descriptionText.text = quest.Description;

        if (quest.Reward == null)
        {
            _questRewardText.text = "";
        }
        else
        {
            switch (quest.Reward.RewardType)
            {
                case (EQuestRewardType.Gold):
                    _questRewardText.text = $"퀘스트 보상: {quest.Reward.Amount} 골드";
                    break;

                case (EQuestRewardType.Item):
                    string itemName = quest.Reward.RewardItem != null ? quest.Reward.RewardItem.DisplayName : "아이템";
                    _questRewardText.text = $"퀘스트 보상: {quest.Reward.RewardItem.DisplayName} {quest.Reward.Amount}개";
                    break;
            }
        }
        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        if (_acceptButton == null || _acceptButtonText == null) return;

        if (_questData == null || QuestManager.Instance == null)
        {
            _acceptButton.interactable = false;
            _acceptButtonText.text = "";
            if (_acceptButtonImage != null)
            {
                _acceptButtonImage.color = Color.white;
            }
            return;
        }

        QuestManager questManager = QuestManager.Instance;

        bool hasQuest = questManager.HasQuest(_questData.QuestId);
        bool canComplete = questManager.CanCompleteQuest(_questData.QuestId);
        bool canAccept = questManager.CanAcceptQuest(_questData);

        if (hasQuest)
        {
            if (canComplete)
            {
                _acceptButton.interactable = true;

                if (_acceptButtonImage != null)
                {
                    _acceptButtonImage.color = Color.yellow;
                }

                _acceptButtonText.text = "완료 가능!";
            }
            else
            {
                _acceptButton.interactable = false;

                if (_acceptButtonImage != null)
                {
                    _acceptButtonImage.color = Color.red;
                }

                _acceptButtonText.text = "진행 중...";
            }
        }
        else
        {
            if (canAccept)
            {
                _acceptButton.interactable = true;

                if (_acceptButtonImage != null)
                {
                    _acceptButtonImage.color = Color.green;
                }

                _acceptButtonText.text = "퀘스트 수락";
            }
            else
            {
                _acceptButton.interactable = false;

                if (_acceptButtonImage != null)
                {
                    _acceptButtonImage.color = Color.gray;
                }

                _acceptButtonText.text = "퀘스트 완료!";
            }
        }
    }

    public void OnClickAcceptButton()
    {
        _uiQuestBoard.OnQuestSlotClicked(_questData);
    }
}
