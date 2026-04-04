
public static class QuestReportItemHelper
{
    public static int AddItemAndReportQuest(PlayerInventoryAbility inventory, ItemDataSO item, int amount)
    {
        if (inventory == null || item == null || amount <= 0)
            return 0;

        int before = inventory.GetItemCount(item);
        inventory.AddItem(item, amount);
        int after = inventory.GetItemCount(item);

        int addedAmount = after - before;

        if (addedAmount > 0)
        {
            QuestManager.Instance?.ReportItemCollected(item.Id, addedAmount);
        }

        return addedAmount;
    }
}
