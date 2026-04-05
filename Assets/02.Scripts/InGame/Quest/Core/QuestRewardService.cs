
using System.Collections.Generic;

public class QuestRewardService
{
    private readonly Dictionary<EQuestRewardType, IQuestRewardHandler> _rewardHandlers;

    public QuestRewardService(PlayerInventoryAbility inventory)
    {
        _rewardHandlers = new Dictionary<EQuestRewardType, IQuestRewardHandler>
        {
            { EQuestRewardType.Gold, new QuestGoldRewardHandler() },
            { EQuestRewardType.Item, new QuestItemRewardHandler(inventory) },
            { EQuestRewardType.Friendship, new QuestFriendshipRewardHandler() }
        };
    }

    public void GiveReward(QuestRewardData rewardData)
    {
        if (rewardData == null || rewardData.Rewards == null) return;

        foreach (var reward in rewardData.Rewards)
        {
            if (reward == null || !reward.IsValid()) continue;

            if (_rewardHandlers.TryGetValue(reward.RewardType, out var handler))
            {
                handler.HandleReward(reward);
            }
        }
    }
}
