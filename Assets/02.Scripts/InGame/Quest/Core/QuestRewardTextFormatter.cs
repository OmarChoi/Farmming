using System.Collections.Generic;

public static class QuestRewardTextFormatter
{
    public static string BuildQuestReward(QuestRewardData rewardData)
    {
        if (rewardData == null || rewardData.Rewards == null || rewardData.Rewards.Count == 0)
        {
            return string.Empty;
        }

        List<string> rewardTexts = new();

        foreach (QuestRewardEntry reward in rewardData.Rewards)
        {
            string text = BuildSingleRewardText(reward);

            if (!string.IsNullOrEmpty(text))
            {
                rewardTexts.Add(text);
            }
        }

        if (rewardTexts.Count == 0)
        {
            return string.Empty;
        }

        return "퀘스트 보상: " + string.Join(", ", rewardTexts);
    }

    private static string BuildSingleRewardText(QuestRewardEntry reward)
    {
        if (reward == null || !reward.IsValid()) return string.Empty;

        switch (reward.RewardType)
        {
            case EQuestRewardType.Gold:
                return $"{reward.Amount} 골드";

            case EQuestRewardType.Item:
                string itemName = reward.RewardItem != null ? reward.RewardItem.DisplayName : "아이템";
                return $"{itemName} {reward.Amount}개";

            case EQuestRewardType.Friendship:
                return $"친밀도 +{reward.Amount}";

            default:
                return string.Empty;
        }
    }
}
