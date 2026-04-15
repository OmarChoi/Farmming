using UnityEngine;

public class QuestHelperUnlockRewardHandler : IQuestRewardHandler
{
    public EQuestRewardType RewardType => EQuestRewardType.HelperUnlock;

    private PlayerHelperInventoryAbility _helperInventory;

    public QuestHelperUnlockRewardHandler(PlayerHelperInventoryAbility helperInventory)
    {
        _helperInventory = helperInventory;
    }

    public void HandleReward(QuestRewardEntry reward)
    {
        if (_helperInventory == null || reward == null || reward.RewardHelper == null) return;

        _helperInventory.AddHelper(reward.RewardHelper);
    }
}
