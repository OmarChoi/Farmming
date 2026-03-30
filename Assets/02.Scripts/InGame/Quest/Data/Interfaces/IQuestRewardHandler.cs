
public interface IQuestRewardHandler
{
    EQuestRewardType RewardType { get; }
    void HandleReward(QuestRewardEntry reward);
}
