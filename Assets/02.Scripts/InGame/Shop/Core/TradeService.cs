
public class TradeService
{
    private readonly PlayerInventoryAbility _playerInventory;

    public TradeService(PlayerController player)
    {
        _playerInventory = player.GetAbility<PlayerInventoryAbility>();
    }

    public bool Buy(ShopData shopData, ItemDataSO item, int amount = 1)
    {
        if (_playerInventory == null) return false;
        if (shopData == null || item == null || amount <= 0) return false;
        if (!shopData.SellItems.Contains(item)) return false;
        if (CurrencyManager.Instance == null) return false;

        int totalCost = item.BuyCost * amount;
        if (!CurrencyManager.Instance.TrySpendGold(totalCost)) return false;

        _playerInventory.AddItem(item, amount);

        return true;
    }

    public bool Sell(int slotIndex, int amount = 1)
    {
        if (_playerInventory == null) return false;
        if (slotIndex < 0 || slotIndex >= _playerInventory.SlotCount) return false;
        if (CurrencyManager.Instance == null) return false;

        InventorySlot slot = _playerInventory.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty) return false;
        if (amount <= 0 || slot.Count < amount) return false;

        ItemDataSO item = slot.Item;
        int basePrice = item.SellCost * amount;
        int finalPrice = WorldEffectManager.Instance != null
            ? WorldEffectManager.ApplyMultiplier(basePrice, WorldEffectManager.Instance.GetSellPriceMultiplier())
            : basePrice;
        CurrencyManager.Instance.AddGold(finalPrice);
        _playerInventory.RemoveAt(slotIndex, amount);
        return true;
    }
}
