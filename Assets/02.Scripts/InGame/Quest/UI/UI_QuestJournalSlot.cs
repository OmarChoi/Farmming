using TMPro;
using UnityEngine;

public class UI_QuestJournalSlot : MonoBehaviour
{
    [Header("퀘스트 슬롯 텍스트")]
    [SerializeField] private TextMeshProUGUI _questNameText;
    [SerializeField] private TextMeshProUGUI _questTargetText;
    [SerializeField] private TextMeshProUGUI _questProgressText;
    [SerializeField] private TextMeshProUGUI _questStatusText;
    [SerializeField] private TextMeshProUGUI _questRewardText;

    private UI_QuestJournal _uiQuestJournal;
    private int _slotIndex;
    private QuestRuntimeData _questRuntimeData;

    public int SlotIndex => _slotIndex;
    public QuestRuntimeData QuestRuntimeData => _questRuntimeData;

    public void Init(UI_QuestJournal uiQuestJournal, int index)
    {
        _uiQuestJournal = uiQuestJournal;
        _slotIndex = index;
    }

    public void Refresh(QuestRuntimeData runtimeData)
    {
        _questRuntimeData = runtimeData;

        if (runtimeData == null || runtimeData.QuestData == null)
        {
            _questNameText.text = "";
            _questTargetText.text = "";
            _questProgressText.text = "";
            _questStatusText.text = "";
            _questRewardText.text = "";
            return;
        }

        QuestDataSO quest = runtimeData.QuestData;

        _questNameText.text = quest.QuestName;

        switch (quest.ObjectiveType)
        {
            case EQuestObjectiveType.BreakObject:
                _questTargetText.text = $"{quest.TargetId} {quest.RequiredAmount}만큼 캐기";
                break;

            default:
                _questTargetText.text = "";
                break;
        }

        _questProgressText.text = $"진행도: {runtimeData.CurrentAmount} / {quest.RequiredAmount}";

        switch (runtimeData.Status)
        {
            case EQuestStatus.InProgress:
                _questStatusText.text = "상태: 진행 중";
                break;

            case EQuestStatus.CanComplete:
                _questStatusText.text = "상태: 완료 가능";
                break;

            case EQuestStatus.Completed:
                _questStatusText.text = "상태: 완료";
                break;

            default:
                _questStatusText.text = "";
                break;
        }

        if (quest.Reward == null)
        {
            _questRewardText.text = "";
        }
        else
        {
            switch (quest.Reward.RewardType)
            {
                case EQuestRewardType.Gold:
                    _questRewardText.text = $"퀘스트 보상: {quest.Reward.Amount} 골드";
                    break;

                case EQuestRewardType.Item:
                    string itemName = quest.Reward.RewardItem != null ? quest.Reward.RewardItem.DisplayName : "아이템";
                    _questRewardText.text = $"퀘스트 보상: {itemName} {quest.Reward.Amount}개";
                    break;
            }
        }
    }
}
