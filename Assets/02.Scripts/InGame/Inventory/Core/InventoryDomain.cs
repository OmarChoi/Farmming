using System;
using System.Collections.Generic;

public class InventoryDomain
{
    private List<InventorySlot> _slots;

    public int SlotCount => _slots.Count;

    public event Action<int> OnSlotChanged;
    public event Action OnInventoryResized;

    public InventoryDomain()
    {
        _slots = new List<InventorySlot>();
    }

    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= _slots.Count) return null;
        return _slots[index];
    }

    /// 아이템 추가. 기존 스택 → 새 슬롯 순으로 채운다
    public void AddItem(ItemDataSO item, int amount = 1)
    {
        int remaining = amount;

        // 1) 같은 아이템이 있는 슬롯에 먼저 스택
        for (int i = 0; i < _slots.Count && remaining > 0; i++)
        {
            var slot = _slots[i];
            if (slot.IsEmpty || slot.Item != item) continue;

            int canAdd = item.MaxStack - slot.Count;
            if (canAdd <= 0) continue;

            int toAdd = Math.Min(remaining, canAdd);
            slot.TryAdd(item, toAdd);
            remaining -= toAdd;
            OnSlotChanged?.Invoke(i);
        }

        // 2) 빈 슬롯이 없으면 새 슬롯 생성
        while (remaining > 0)
        {
            var newSlot = new InventorySlot();
            int toAdd = Math.Min(remaining, item.MaxStack);
            newSlot.TryAdd(item, toAdd);
            _slots.Add(newSlot);
            remaining -= toAdd;
            OnInventoryResized?.Invoke();
            OnSlotChanged?.Invoke(_slots.Count - 1);
        }
    }

    public void SwapSlots(int from, int to)
    {
        if (from < 0 || from >= _slots.Count) return;
        if (to < 0 || to >= _slots.Count) return;
        if (from == to) return;

        var fromSlot = _slots[from];
        var toSlot = _slots[to];

        // 같은 아이템이면 스택 합치기 시도
        if (!fromSlot.IsEmpty && !toSlot.IsEmpty
            && fromSlot.Item == toSlot.Item
            && toSlot.Count < toSlot.Item.MaxStack)
        {
            int canAdd = toSlot.Item.MaxStack - toSlot.Count;
            int toMove = Math.Min(fromSlot.Count, canAdd);
            toSlot.TryAdd(toSlot.Item, toMove);
            fromSlot.Remove(toMove);

            if (fromSlot.IsEmpty)
            {
                int toIndex = from < to ? to - 1 : to;
                _slots.RemoveAt(from);
                OnSlotChanged?.Invoke(toIndex);
                OnInventoryResized?.Invoke();
                return;
            }
        }
        else
        {
            fromSlot.SwapWith(toSlot);
        }

        OnSlotChanged?.Invoke(from);
        OnSlotChanged?.Invoke(to);
    }

    public void RemoveAt(int index, int amount = 1)
    {
        if (index < 0 || index >= _slots.Count) return;
        _slots[index].Remove(amount);

        if (_slots[index].IsEmpty)
        {
            _slots.RemoveAt(index);
            OnInventoryResized?.Invoke();
        }
        else
        {
            OnSlotChanged?.Invoke(index);
        }
    }

    public void ReplaceAll(IEnumerable<InventorySlot> newSlots)
    {
        _slots.Clear();
        _slots.AddRange(newSlots);
        OnInventoryResized?.Invoke();
    }
}