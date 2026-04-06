using System;

[Serializable]
public struct QuestItemProgressEntry
{
    public int ItemId;
    public int CurrentAmount;

    public QuestItemProgressEntry(int itemId, int currentAmount)
    {
        ItemId = itemId;
        CurrentAmount = currentAmount;
    }
}
