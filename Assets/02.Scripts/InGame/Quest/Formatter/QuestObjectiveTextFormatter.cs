using System.Collections.Generic;

public class QuestObjectiveTextFormatter
{
    private static NpcDataContainerSO _npcDataContainer;
    public static void SetNpcDataContainer(NpcDataContainerSO container)
    {
        _npcDataContainer = container;
    }

    private static string GetNpcDisplayName(string npcId)
    {
        if (_npcDataContainer == null || string.IsNullOrEmpty(npcId)) return npcId;
        return _npcDataContainer.GetNpcName(npcId);
    }

    public static string BuildTargetText(QuestDataSO questData)
    {
        if (questData == null) return string.Empty;

        switch (questData.ObjectiveType)
        {
            case EQuestObjectiveType.BreakObject:
                return $"{questData.TargetId} {questData.RequiredAmount}만큼 캐기";

            case EQuestObjectiveType.CollectItem:
                return $"{BuildItemRequirementText(questData.ItemRequirements)} 가져오기";

            case EQuestObjectiveType.DeliverItem:
                return $"{GetNpcDisplayName(questData.TargetNpcId)}에게 {BuildItemRequirementText(questData.ItemRequirements)} 가져다주기";

            case EQuestObjectiveType.TalkToNpc:
                return $"{GetNpcDisplayName(questData.TargetNpcId)} 찾아가기";

            case EQuestObjectiveType.DryFarmTile:
                return $"땅을 {questData.RequiredAmount}만큼 경작하기";

            case EQuestObjectiveType.PlantSeed:
                return $"{questData.TargetId}를 {questData.RequiredAmount}만큼 심기";

            case EQuestObjectiveType.WaterFarmTile:
                return $"밭에 {questData.RequiredAmount}만큼 물주기";

            default:
                return string.Empty;
        }
    }

    public static string BuildProgressText(QuestRuntimeData runtimeData)
    {
        if (runtimeData == null || runtimeData.QuestData == null)
            return string.Empty;

        QuestDataSO questData = runtimeData.QuestData;

        switch (questData.ObjectiveType)
        {
            case EQuestObjectiveType.BreakObject:
            case EQuestObjectiveType.TalkToNpc:
            case EQuestObjectiveType.DryFarmTile:
            case EQuestObjectiveType.PlantSeed:
            case EQuestObjectiveType.WaterFarmTile:
                return $"진행도: {runtimeData.CurrentAmount} / {questData.RequiredAmount}";

            case EQuestObjectiveType.CollectItem:
            case EQuestObjectiveType.DeliverItem:
                return $"진행도: {BuildItemProgressText(runtimeData)}";

            default:
                return string.Empty;
        }
    }

    private static string BuildItemRequirementText(List<QuestItemRequirementEntry> requirements)
    {
        if (requirements == null || requirements.Count == 0) return string.Empty;

        List<string> parts = new();

        foreach (QuestItemRequirementEntry requirement in requirements)
        {
            if (requirement.Item == null) continue;

            string itemName = requirement.ItemName;
            parts.Add($"{itemName} {requirement.Amount}개");
        }

        return string.Join(", ", parts);
    }

    private static string BuildItemProgressText(QuestRuntimeData runtimeData)
    {
        if (runtimeData == null || runtimeData.QuestData == null) return string.Empty;

        List<QuestItemRequirementEntry> requirements = runtimeData.QuestData.ItemRequirements;
        if (requirements == null || requirements.Count == 0) return string.Empty;

        List<string> parts = new();

        foreach (QuestItemRequirementEntry requirement in requirements)
        {
            if (requirement.Item == null) continue;

            int itemId = requirement.ItemId;
            int currentAmount = runtimeData.GetItemProgress(itemId);
            parts.Add($"{requirement.ItemName} {currentAmount}/{requirement.Amount}");
        }

        return string.Join(", ", parts);
    }
}
