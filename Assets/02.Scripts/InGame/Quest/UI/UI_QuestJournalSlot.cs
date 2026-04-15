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
    [SerializeField] private TextMeshProUGUI _questRemainingDaysText;

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
            if (_questRemainingDaysText != null) _questRemainingDaysText.text = "";
            return;
        }

        QuestDataSO quest = runtimeData.QuestData;

        _questNameText.text = quest.QuestName;
        _questTargetText.text = QuestObjectiveTextFormatter.BuildTargetText(quest);
        _questProgressText.text = QuestObjectiveTextFormatter.BuildProgressText(runtimeData);
        _questStatusText.text = QuestStatusTextFormatter.BuildStatusText(runtimeData);
        _questRewardText.text = QuestRewardTextFormatter.BuildQuestReward(quest.Reward);

        if (_questRemainingDaysText != null)
        {
            if (quest.QuestCategory == EQuestCategory.ForcedTimed && runtimeData.ExpireDay > 0)
            {
                int remaining = Mathf.Max(0, runtimeData.ExpireDay - TimeEvents.CurrentDay);
                _questRemainingDaysText.text = $"남은 기간: {remaining}일";
            }
            else
            {
                _questRemainingDaysText.text = "";
            }
        }
    }
}
