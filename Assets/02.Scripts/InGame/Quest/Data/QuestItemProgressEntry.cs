using System;

[Serializable]
public class QuestItemProgressEntry
{
    public int ItemId;
    public int CurrentAmount;

    public QuestItemProgressEntry(int itemId, int currentAmount)
    {
        ItemId = itemId;
        CurrentAmount = currentAmount;
    }
}
