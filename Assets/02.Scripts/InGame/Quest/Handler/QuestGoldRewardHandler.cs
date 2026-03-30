using UnityEngine;

public class QuestGoldRewardHandler : IQuestRewardHandler
{
    public EQuestRewardType RewardType => EQuestRewardType.Gold;

    public void HandleReward(QuestRewardEntry reward)
    {
        CurrencyManager.Instance?.AddGold(reward.Amount);
    }
}
