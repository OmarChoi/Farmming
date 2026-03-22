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

    private UI_QuestBoard _uiQuestBoard;
    private int _slotIndex;
    private QuestDataSO _questData;

    public int SlotIndex => _slotIndex;
    public QuestDataSO QuestData => _questData;

    public void Init(UI_QuestBoard uiQuestBoard, int index)
    {
        _uiQuestBoard = uiQuestBoard;
        _slotIndex = index;
    }

    public void Refresh(QuestDataSO quest)
    {
        _questData = quest;

        if (quest == null)
        {
            _questNameText.text = "";
            _descriptionText.text = "";
            _questRewardText.text = "";
            return;
        }
        _questNameText.text = $"{quest.QuestName}";
        _descriptionText.text = $"Cost: {quest.Description}";
        _questRewardText.text = $"{quest.Reward}";
    }

    public void OnClickAcceptButton()
    {
        _uiQuestBoard.OnQuestSlotClicked(this);
    }
}
