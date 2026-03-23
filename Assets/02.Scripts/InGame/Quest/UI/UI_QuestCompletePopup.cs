using TMPro;
using UnityEngine;

public class UI_QuestCompletePopup : MonoBehaviour
{
    [Header("퀘스트 완료 팝업")]
    [SerializeField] private GameObject _completePopup;
    [SerializeField] private TextMeshProUGUI _completeRewardText;

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted += ShowCompletePopup;
        }
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted -= ShowCompletePopup;
        }
    }

    private void ShowCompletePopup(QuestRuntimeData quest)
    {
        if (quest == null || quest.QuestData == null || quest.QuestData.Reward == null) return;

        _completePopup.SetActive(true);
        var reward = quest.QuestData.Reward;
        switch (reward.RewardType)
        {
            case (EQuestRewardType.Gold):
                _completeRewardText.text = $"퀘스트 보상: {reward.Amount} 골드";
                break;
            case (EQuestRewardType.Item):
                _completeRewardText.text = $"퀘스트 보상: {reward.RewardItem.DisplayName} {reward.Amount}개";
                break;
        }
    }

    public void HideCompletePopup()
    {
        _completePopup.SetActive(false);
    }
}
