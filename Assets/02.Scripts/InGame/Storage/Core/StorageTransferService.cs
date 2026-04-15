using System;

/// 인벤토리 ↔ 창고 간 아이템 전송 서비스.
public class StorageTransferService
{
    protected readonly SlotContainerDomain _inventory;
    protected readonly StorageDomain _storage;
    public virtual bool RequiresRestoreBeforeCrossSwap => true;

    public StorageTransferService(SlotContainerDomain inventory, StorageDomain storage)
    {
        _inventory = inventory;
        _storage = storage;
    }

    /// 인벤토리 슬롯 → 창고로 이동. amount=0이면 전체, 그 외는 지정 수량.
    public virtual bool MoveToStorage(int inventorySlotIndex, int amount = 0)
    {
        var slot = _inventory.GetSlot(inventorySlotIndex);
        if (slot == null || slot.IsEmpty) return false;

        int available = CalculateAvailableSpace(_storage, slot.Item);
        if (available <= 0) return false;

        int desired = amount <= 0 ? slot.Count : Math.Min(amount, slot.Count);
        int toMove = Math.Min(desired, available);
        var item = slot.Item;

        _inventory.RemoveAt(inventorySlotIndex, toMove);
        _storage.AddItem(item, toMove);
        return true;
    }

    /// 창고 슬롯 → 인벤토리로 이동. amount=0이면 전체, 그 외는 지정 수량.
    public virtual bool MoveToInventory(int storageSlotIndex, int amount = 0)
    {
        var slot = _storage.GetSlot(storageSlotIndex);
        if (slot == null || slot.IsEmpty) return false;

        int desired = amount <= 0 ? slot.Count : Math.Min(amount, slot.Count);
        var item = slot.Item;

        _storage.RemoveAt(storageSlotIndex, desired);
        _inventory.AddItem(item, desired);
        return true;
    }

    public virtual bool AddHeldItemToInventory(ItemDataSO item, int amount, int inventorySlotIndex = -1)
    {
        if (item == null || amount <= 0) return false;

        if (inventorySlotIndex >= 0)
            _inventory.AddItemToSlot(item, inventorySlotIndex, amount);
        else
            _inventory.AddItem(item, amount);
        return true;
    }

    /// 인벤토리 슬롯과 창고 슬롯 간 스왑.
    public virtual void SwapAcross(int inventorySlotIndex, int storageSlotIndex)
    {
        var invSlot = _inventory.GetSlot(inventorySlotIndex);
        var stoSlot = _storage.GetSlot(storageSlotIndex);
        if (invSlot == null || stoSlot == null) return;

        // 같은 아이템이면 스택 합치기
        if (!invSlot.IsEmpty && !stoSlot.IsEmpty
            && invSlot.Item == stoSlot.Item
            && stoSlot.Count < stoSlot.Item.MaxStack)
        {
            int canAdd = stoSlot.Item.MaxStack - stoSlot.Count;
            int toMove = Math.Min(invSlot.Count, canAdd);
            stoSlot.TryAdd(stoSlot.Item, toMove);
            invSlot.Remove(toMove);
        }
        else
        {
            invSlot.SwapWith(stoSlot);
        }

        // 직접 슬롯을 조작했으므로 양쪽 도메인에 변경 알림
        _inventory.NotifySlotChanged(inventorySlotIndex);
        _storage.NotifySlotChanged(storageSlotIndex);
    }

    // === 창고 내부 조작 (드래그/스왑/분할) ===

    /// 창고 내부 슬롯 스왑
    public virtual void SwapInStorage(int from, int to)
    {
        _storage.SwapSlots(from, to);
    }

    /// 드래그 시작: 창고 슬롯에서 아이템을 들어올림 (도메인에서 제거)
    public virtual void PickUpFromStorage(int slotIndex, out ItemDataSO item, out int count)
    {
        var slot = _storage.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty)
        {
            item = null;
            count = 0;
            return;
        }

        item = slot.Item;
        count = slot.Count;
        _storage.RemoveAt(slotIndex, count);
    }

    /// 드래그 종료/취소: 들고 있던 아이템을 창고 슬롯에 내려놓음
    public virtual void PutDownInStorage(int slotIndex, ItemDataSO item, int count)
    {
        if (item == null || count <= 0) return;

        var slot = _storage.GetSlot(slotIndex);
        if (slot == null) return;

        if (slot.IsEmpty)
        {
            slot.TryAdd(item, count);
        }
        else if (slot.Item == item)
        {
            int canAdd = item.MaxStack - slot.Count;
            int toAdd = Math.Min(count, canAdd);
            if (toAdd > 0) slot.TryAdd(item, toAdd);
        }

        _storage.NotifySlotChanged(slotIndex);
    }

    /// 들고 있는 아이템을 특정 창고 슬롯에 직접 배치
    public virtual bool AddHeldItemToStorageSlot(ItemDataSO item, int storageSlotIndex, int amount)
    {
        if (item == null || amount <= 0) return false;
        return _storage.AddItemToSlot(item, storageSlotIndex, amount, fallbackToAuto: false);
    }

    /// 분할 드래그 시작
    public virtual int SplitHalfInStorage(int index)
    {
        return _storage.SplitHalf(index);
    }

    /// 분할 결과 배치
    public virtual void PlaceSplitInStorage(int sourceIndex, int targetIndex, ItemDataSO item, int amount)
    {
        _storage.PlaceSplit(sourceIndex, targetIndex, item, amount);
    }

    private int CalculateAvailableSpace(SlotContainerDomain container, ItemDataSO item)
    {
        int space = 0;
        for (int i = 0; i < container.SlotCount; i++)
        {
            var slot = container.GetSlot(i);
            if (slot.IsEmpty)
                space += item.MaxStack;
            else if (slot.Item == item)
                space += item.MaxStack - slot.Count;
        }
        return space;
    }
}
