using UnityEngine;

public class QuestItemRewardHandler : IQuestRewardHandler
{
    private PlayerInventoryAbility _inventory;

    public QuestItemRewardHandler(PlayerInventoryAbility inventory)
    {
        _inventory = inventory;
    }

    public EQuestRewardType RewardType => EQuestRewardType.Item;

    public void HandleReward(QuestRewardEntry reward)
    {
        _inventory.AddItem(reward.RewardItem, reward.Amount);
    }
}
