using System;
using System.Collections.Generic;

public class InventoryDomain
{
    private const int INITIAL_SIZE = 16;
    private const int EXPAND_SIZE = 4;

    private readonly List<InventorySlot> _slots;

    public int SlotCount => _slots.Count;

    public event Action<int> OnSlotChanged;
    public event Action OnInventoryResized;

    public InventoryDomain()
    {
        _slots = new List<InventorySlot>(INITIAL_SIZE);
        for (int i = 0; i < INITIAL_SIZE; i++)
            _slots.Add(new InventorySlot());
    }

    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= _slots.Count) return null;
        return _slots[index];
    }

    /// 아이템 추가. 기존 스택 → 빈 슬롯 → 확장 순으로 채운다
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

        // 2) 빈 슬롯에 채우기
        for (int i = 0; i < _slots.Count && remaining > 0; i++)
        {
            if (!_slots[i].IsEmpty) continue;

            int toAdd = Math.Min(remaining, item.MaxStack);
            _slots[i].TryAdd(item, toAdd);
            remaining -= toAdd;
            OnSlotChanged?.Invoke(i);
        }

        // 3) 슬롯이 모두 찬 경우 4칸 확장 후 채우기
        while (remaining > 0)
        {
            Expand();
            int firstNew = _slots.Count - EXPAND_SIZE;
            int toAdd = Math.Min(remaining, item.MaxStack);
            _slots[firstNew].TryAdd(item, toAdd);
            remaining -= toAdd;
            OnSlotChanged?.Invoke(firstNew);
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
        }
        else
        {
            fromSlot.SwapWith(toSlot);
        }

        OnSlotChanged?.Invoke(from);
        OnSlotChanged?.Invoke(to);
    }

    /// 슬롯에서 절반을 분리하여 반환. 실패 시 0 반환
    public int SplitHalf(int index)
    {
        if (index < 0 || index >= _slots.Count) return 0;

        var slot = _slots[index];
        if (slot.IsEmpty || slot.Count < 2) return 0;

        int half = slot.Count / 2;
        slot.Remove(half);
        OnSlotChanged?.Invoke(index);
        return half;
    }

    /// 분리한 아이템을 대상 슬롯에 배치. 실패 시 원래 슬롯에 복구
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
            OnSlotChanged?.Invoke(targetIndex);
        }
        else if (targetSlot.Item == item)
        {
            int canAdd = item.MaxStack - targetSlot.Count;
            int toAdd = Math.Min(amount, canAdd);
            if (toAdd > 0)
            {
                targetSlot.TryAdd(item, toAdd);
                OnSlotChanged?.Invoke(targetIndex);
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

    private void RestoreSplit(int sourceIndex, ItemDataSO item, int amount)
    {
        if (sourceIndex < 0 || sourceIndex >= _slots.Count) return;
        _slots[sourceIndex].TryAdd(item, amount);
        OnSlotChanged?.Invoke(sourceIndex);
    }

    public void RemoveAt(int index, int amount = 1)
    {
        if (index < 0 || index >= _slots.Count) return;

        _slots[index].Remove(amount);
        OnSlotChanged?.Invoke(index);
        TryShrink();
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

        OnInventoryResized?.Invoke();
    }

    private void Expand()
    {
        for (int i = 0; i < EXPAND_SIZE; i++)
            _slots.Add(new InventorySlot());
        OnInventoryResized?.Invoke();
    }

    /// 마지막 4칸이 모두 비어있으면 제거 (최소 INITIAL_SIZE 유지)
    private void TryShrink()
    {
        while (_slots.Count > INITIAL_SIZE)
        {
            int tailStart = _slots.Count - EXPAND_SIZE;
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

            _slots.RemoveRange(tailStart, EXPAND_SIZE);
            OnInventoryResized?.Invoke();
        }
    }
}