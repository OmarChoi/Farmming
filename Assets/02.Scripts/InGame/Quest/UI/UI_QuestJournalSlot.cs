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
                _questTargetText.text = $"{quest.TargetObjectId} {quest.RequiredAmount}만큼 캐기";
                break;

            case EQuestObjectiveType.CollectItem:
                _questTargetText.text = $"{quest.TargetItemId} {quest.RequiredAmount}만큼 가져오기";
                break;

            case EQuestObjectiveType.DeliverItem:
                _questTargetText.text = $"{quest.TargetNpcId}에게 {quest.TargetItemId} {quest.RequiredAmount}개 가져다주기";
                break;

            case EQuestObjectiveType.TalkToNpc:
                _questTargetText.text = $"{quest.TargetNpcId} 찾아가기";
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

        _questRewardText.text = QuestRewardTextFormatter.BuildQuestReward(quest.Reward);
    }
}
