using System;
using System.Collections.Generic;

/// 슬롯 기반 컨테이너의 공통 베이스.
/// InventoryDomain(자동 확장)과 StorageDomain(고정 크기) 모두 이 클래스를 상속한다.
public class SlotContainerDomain
{
    protected readonly List<InventorySlot> _slots;

    public int SlotCount => _slots.Count;

    public event Action<int> OnSlotChanged;
    public event Action OnSlotCountChanged;

    public SlotContainerDomain(int initialSize)
    {
        _slots = new List<InventorySlot>(initialSize);
        for (int i = 0; i < initialSize; i++)
            _slots.Add(new InventorySlot());
    }

    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= _slots.Count) return null;
        return _slots[index];
    }

    /// 아이템 추가. 기존 스택 → 빈 슬롯 순으로 채운다.
    /// 모두 넣었으면 true, 공간 부족으로 남은 게 있으면 false.
    public virtual bool AddItem(ItemDataSO item, int amount = 1)
    {
        return FillExistingSlots(item, amount) == 0;
    }

    /// 기존 스택 → 빈 슬롯 순으로 채우고, 못 넣은 나머지 수량을 반환한다.
    protected int FillExistingSlots(ItemDataSO item, int amount)
    {
        int remaining = amount;

        for (int i = 0; i < _slots.Count && remaining > 0; i++)
        {
            var slot = _slots[i];
            if (slot.IsEmpty || slot.Item != item) continue;

            int canAdd = item.MaxStack - slot.Count;
            if (canAdd <= 0) continue;

            int toAdd = Math.Min(remaining, canAdd);
            slot.TryAdd(item, toAdd);
            remaining -= toAdd;
            NotifySlotChanged(i);
        }

        for (int i = 0; i < _slots.Count && remaining > 0; i++)
        {
            if (!_slots[i].IsEmpty) continue;

            int toAdd = Math.Min(remaining, item.MaxStack);
            _slots[i].TryAdd(item, toAdd);
            remaining -= toAdd;
            NotifySlotChanged(i);
        }

        return remaining;
    }

    public void SwapSlots(int from, int to)
    {
        if (from < 0 || from >= _slots.Count) return;
        if (to < 0 || to >= _slots.Count) return;
        if (from == to) return;

        var fromSlot = _slots[from];
        var toSlot = _slots[to];

        if (!fromSlot.IsEmpty && !toSlot.IsEmpty
            && fromSlot.Item == toSlot.Item
            && toSlot.Count < toSlot.Item.MaxStack)
        {
            int canAdd = toSlot.Item.MaxStack - toSlot.Count;
            int toMove = Math.Min(fromSlot.Count, canAdd);
            toSlot.TryAdd(toSlot.Item, toMove);
            fromSlot.Remove(toMove);
        }
        else
        {
            fromSlot.SwapWith(toSlot);
        }

        NotifySlotChanged(from);
        NotifySlotChanged(to);
    }

    public int SplitHalf(int index)
    {
        if (index < 0 || index >= _slots.Count) return 0;

        var slot = _slots[index];
        if (slot.IsEmpty || slot.Count < 2) return 0;

        int half = slot.Count / 2;
        slot.Remove(half);
        NotifySlotChanged(index);
        return half;
    }

    public void PlaceSplit(int sourceIndex, int targetIndex, ItemDataSO item, int amount)
    {
        if (targetIndex < 0 || targetIndex >= _slots.Count)
        {
            RestoreSplit(sourceIndex, item, amount);
            return;
        }

        var targetSlot = _slots[targetIndex];

        if (targetSlot.IsEmpty)
        {
            targetSlot.TryAdd(item, amount);
            NotifySlotChanged(targetIndex);
        }
        else if (targetSlot.Item == item)
        {
            int canAdd = item.MaxStack - targetSlot.Count;
            int toAdd = Math.Min(amount, canAdd);
            if (toAdd > 0)
            {
                targetSlot.TryAdd(item, toAdd);
                NotifySlotChanged(targetIndex);
            }
            int leftover = amount - toAdd;
            if (leftover > 0)
                RestoreSplit(sourceIndex, item, leftover);
        }
        else
        {
            RestoreSplit(sourceIndex, item, amount);
        }
    }

    public virtual void RemoveAt(int index, int amount = 1)
    {
        if (index < 0 || index >= _slots.Count) return;

        _slots[index].Remove(amount);
        NotifySlotChanged(index);
    }

    public int GetItemCount(ItemDataSO item)
    {
        int total = 0;
        foreach (var slot in _slots)
        {
            if (!slot.IsEmpty && slot.Item == item)
                total += slot.Count;
        }
        return total;
    }

    public void ReplaceAll(int totalSlots, IEnumerable<(int index, InventorySlot slot)> filledSlots)
    {
        _slots.Clear();
        for (int i = 0; i < totalSlots; i++)
            _slots.Add(new InventorySlot());

        foreach (var (index, slot) in filledSlots)
        {
            if (index >= 0 && index < _slots.Count)
                _slots[index] = slot;
        }

        NotifySlotCountChanged();
    }

    private void RestoreSplit(int sourceIndex, ItemDataSO item, int amount)
    {
        if (sourceIndex < 0 || sourceIndex >= _slots.Count) return;
        _slots[sourceIndex].TryAdd(item, amount);
        NotifySlotChanged(sourceIndex);
    }

    protected void NotifySlotChanged(int index) => OnSlotChanged?.Invoke(index);
    protected void NotifySlotCountChanged() => OnSlotCountChanged?.Invoke();
}