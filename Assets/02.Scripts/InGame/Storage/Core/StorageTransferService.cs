using System;

/// 인벤토리 ↔ 창고 간 아이템 전송 서비스.
public class StorageTransferService
{
    protected readonly SlotContainerDomain _inventory;
    protected readonly StorageDomain _storage;

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
        _inventory.RemoveAt(inventorySlotIndex, 0);
        _storage.RemoveAt(storageSlotIndex, 0);
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