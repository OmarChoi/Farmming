using System;

[Serializable]
public class InventorySlot
{
    public ItemData Item { get; private set; }
    public int Count { get; private set; }
    public bool IsEmpty => Item == null || Count <= 0;

    public bool CanAccept(ItemData item, int amount = 1)
    {
        if (IsEmpty) return true;
        return Item == item && Count + amount <= Item.MaxStack;
    }

    public bool TryAdd(ItemData item, int amount = 1)
    {
        if (IsEmpty)
        {
            Item = item;
            Count = amount;
            return true;
        }

        if (Item != item || Count + amount > Item.MaxStack)
            return false;

        Count += amount;
        return true;
    }

    public int Remove(int amount = 1)
    {
        if (IsEmpty) return 0;

        int removed = Math.Min(amount, Count);
        Count -= removed;

        if (Count <= 0)
            Clear();

        return removed;
    }

    public void Clear()
    {
        Item = null;
        Count = 0;
    }

    public void SwapWith(InventorySlot other)
    {
        (Item, other.Item) = (other.Item, Item);
        (Count, other.Count) = (other.Count, Count);
    }
}