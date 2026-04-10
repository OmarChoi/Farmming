using System;

/// 네트워크 환경용 StorageTransferService 래퍼.
/// 마스터: 로컬 실행 후 브로드캐스트.
/// 클라이언트: 인벤토리만 로컬 수정, 창고 변경은 RPC 요청.
public class NetworkStorageTransferProxy : StorageTransferService
{
    private readonly StorageSyncHandler _syncHandler;
    public override bool RequiresRestoreBeforeCrossSwap => _syncHandler.IsMaster;

    public NetworkStorageTransferProxy(
        SlotContainerDomain inventory, StorageDomain storage, StorageSyncHandler syncHandler)
        : base(inventory, storage)
    {
        _syncHandler = syncHandler;
    }

    public override bool MoveToStorage(int inventorySlotIndex, int amount = 0)
    {
        if (_syncHandler.IsMaster)
        {
            bool result = base.MoveToStorage(inventorySlotIndex, amount);
            if (result) _syncHandler.BroadcastFullSync();
            return result;
        }

        // 클라이언트: 인벤토리에서 빼고, 마스터에게 창고 추가 요청
        var slot = _inventory.GetSlot(inventorySlotIndex);
        if (slot == null || slot.IsEmpty) return false;

        int desired = amount <= 0 ? slot.Count : Math.Min(amount, slot.Count);
        int itemId = slot.Item.Id;

        _inventory.RemoveAt(inventorySlotIndex, desired);
        _syncHandler.RequestAddItem(itemId, desired);
        return true;
    }

    public override bool MoveToInventory(int storageSlotIndex, int amount = 0)
    {
        if (_syncHandler.IsMaster)
        {
            bool result = base.MoveToInventory(storageSlotIndex, amount);
            if (result) _syncHandler.BroadcastFullSync();
            return result;
        }

        // 클라이언트: 마스터에게 창고에서 빼달라고 요청 → 마스터가 RPC_ReceiveItem으로 지급
        var slot = _storage.GetSlot(storageSlotIndex);
        if (slot == null || slot.IsEmpty) return false;

        int desired = amount <= 0 ? slot.Count : Math.Min(amount, slot.Count);
        _syncHandler.RequestRemoveItem(storageSlotIndex, desired);
        return true;
    }

    public override void SwapAcross(int inventorySlotIndex, int storageSlotIndex)
    {
        if (_syncHandler.IsMaster)
        {
            base.SwapAcross(inventorySlotIndex, storageSlotIndex);
            _syncHandler.BroadcastFullSync();
            return;
        }

        // 클라이언트: 인벤토리 아이템 정보를 보내고, 마스터가 스왑 후 기존 창고 아이템을 돌려줌
        var invSlot = _inventory.GetSlot(inventorySlotIndex);
        int inItemId = 0;
        int inItemCount = 0;

        if (invSlot != null && !invSlot.IsEmpty)
        {
            inItemId = invSlot.Item.Id;
            inItemCount = invSlot.Count;
            _inventory.RemoveAt(inventorySlotIndex, inItemCount);
        }

        _syncHandler.RequestSwap(storageSlotIndex, inventorySlotIndex, inItemId, inItemCount);
    }

    // === 창고 내부 조작 ===

    public override void SwapInStorage(int from, int to)
    {
        base.SwapInStorage(from, to);
        if (_syncHandler.IsMaster)
            _syncHandler.BroadcastFullSync();
    }

    public override void PickUpFromStorage(int slotIndex, out ItemDataSO item, out int count)
    {
        base.PickUpFromStorage(slotIndex, out item, out count);
        if (_syncHandler.IsMaster)
            _syncHandler.BroadcastFullSync();
    }

    public override void PutDownInStorage(int slotIndex, ItemDataSO item, int count)
    {
        base.PutDownInStorage(slotIndex, item, count);
        if (_syncHandler.IsMaster)
            _syncHandler.BroadcastFullSync();
    }

    public override int SplitHalfInStorage(int index)
    {
        int result = base.SplitHalfInStorage(index);
        if (result > 0)
        {
            if (_syncHandler.IsMaster)
                _syncHandler.BroadcastFullSync();
            else
                _syncHandler.RequestSplitHalf(index);
        }
        return result;
    }

    public override void PlaceSplitInStorage(int sourceIndex, int targetIndex, ItemDataSO item, int amount)
    {
        base.PlaceSplitInStorage(sourceIndex, targetIndex, item, amount);
        if (_syncHandler.IsMaster)
            _syncHandler.BroadcastFullSync();
        else if (item != null && amount > 0)
            _syncHandler.RequestPlaceSplit(sourceIndex, targetIndex, item.Id, amount);
    }

    public override bool AddHeldItemToInventory(ItemDataSO item, int amount)
    {
        if (_syncHandler.IsMaster)
            return base.AddHeldItemToInventory(item, amount);

        if (item == null || amount <= 0) return false;

        _syncHandler.RequestGiveHeldItem(item.Id, amount);
        return true;
    }
}
