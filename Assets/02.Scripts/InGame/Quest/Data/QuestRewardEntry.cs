using System;
using UnityEngine;

[Serializable]
public class QuestRewardEntry
{
    public EQuestRewardType RewardType;
    public ItemDataSO RewardItem;
    public int Amount = 1;

    [Header("호감도 보상용")]
    public string TargetNpcId;

    public bool IsValid()
    {
        if (Amount <= 0) return false;

        switch (RewardType)
        {
            case EQuestRewardType.Gold:
                return true;

            case EQuestRewardType.Item:
                return RewardItem != null;

            case EQuestRewardType.Friendship:
                return !string.IsNullOrEmpty(TargetNpcId);

            default:
                return false;
        }
    }
}
