using UnityEngine;

public class QuestFriendshipRewardHandler : IQuestRewardHandler
{
    public EQuestRewardType RewardType => EQuestRewardType.Friendship;

    public void HandleReward(QuestRewardEntry reward)
    {
        NpcFriendshipManager.Instance?.AddFriendship(reward.TargetNpcId, reward.Amount, ENpcFriendshipReason.QuestReward);
    }
}
