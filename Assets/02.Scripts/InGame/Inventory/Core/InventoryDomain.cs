using System;

/// 플레이어 인벤토리. 슬롯이 꽉 차면 자동 확장, 비면 자동 축소.
public class InventoryDomain : SlotContainerDomain
{
    private const int InitialSize = 16;
    private const int ExpandSize = 4;

    public event Action OnInventoryResized
    {
        add => OnSlotCountChanged += value;
        remove => OnSlotCountChanged -= value;
    }

    public InventoryDomain() : base(InitialSize) { }

    /// 아이템 추가. 공간 부족 시 자동 확장하므로 항상 성공한다.
    public override bool AddItem(ItemDataSO item, int amount = 1)
    {
        int remaining = FillExistingSlots(item, amount);

        while (remaining > 0)
        {
            Expand();
            int firstNew = _slots.Count - ExpandSize;
            int toAdd = Math.Min(remaining, item.MaxStack);
            _slots[firstNew].TryAdd(item, toAdd);
            remaining -= toAdd;
            NotifySlotChanged(firstNew);
        }

        return true;
    }

    public bool RemoveItem(ItemDataSO item, int amount)
    {
        if (GetItemCount(item) < amount) return false;

        int remaining = amount;
        for (int i = 0; i < _slots.Count && remaining > 0; i++)
        {
            var slot = _slots[i];
            if (slot.IsEmpty || !IsSameItem(slot.Item, item)) continue;

            int toRemove = Math.Min(remaining, slot.Count);
            slot.Remove(toRemove);
            remaining -= toRemove;
            NotifySlotChanged(i);
        }

        TryShrink();
        return true;
    }

    public override void RemoveAt(int index, int amount = 1)
    {
        base.RemoveAt(index, amount);
        TryShrink();
    }

    private void Expand()
    {
        for (int i = 0; i < ExpandSize; i++)
            _slots.Add(new InventorySlot());
        NotifySlotCountChanged();
    }

    private void TryShrink()
    {
        while (_slots.Count > InitialSize)
        {
            int tailStart = _slots.Count - ExpandSize;
            bool allEmpty = true;
            for (int i = tailStart; i < _slots.Count; i++)
            {
                if (!_slots[i].IsEmpty)
                {
                    allEmpty = false;
                    break;
                }
            }

            if (!allEmpty) break;

            _slots.RemoveRange(tailStart, ExpandSize);
            NotifySlotCountChanged();
        }
    }
}