
public static class QuestTextUtility
{
    private static NpcDataContainerSO _npcDataContainer;

    public static void SetNpcDataContainer(NpcDataContainerSO container)
    {
        _npcDataContainer = container;
    }

    public static string GetNpcDisplayName(string npcId)
    {
        if (_npcDataContainer == null || string.IsNullOrEmpty(npcId)) return npcId;

        return _npcDataContainer.GetNpcName(npcId);
    }
}
