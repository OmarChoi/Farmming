
public class TradeService
{
    private PlayerInventoryAbility _playerInventory;

    public TradeService(PlayerInventoryAbility playerInventory)
    {
        _playerInventory = playerInventory;
    }

    public bool Buy(ShopData shopData, ItemDataSO item, int amount = 1)
    {
        if (shopData == null || item == null || amount <= 0) return false;

        if (!shopData.SellItems.Contains(item)) return false;

        if (!CurrencyManager.Instance.TrySpendGold(item.BuyCost * amount)) return false;

        _playerInventory.AddItem(item, amount);
        return true;
    }

    public bool Sell(int slotIndex, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= _playerInventory.SlotCount) return false;

        InventorySlot slot = _playerInventory.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty) return false;
        if (amount <= 0 || slot.Count < amount) return false;

        ItemDataSO item = slot.Item;
        CurrencyManager.Instance.AddGold(item.SellCost);
        _playerInventory.RemoveAt(slotIndex, amount);
        return true;
    }
}
