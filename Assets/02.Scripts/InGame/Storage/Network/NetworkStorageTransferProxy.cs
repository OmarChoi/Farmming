using System;

/// 네트워크 환경용 StorageTransferService 래퍼.
/// 마스터: 로컬 실행 후 브로드캐스트.
/// 클라이언트: 인벤토리만 로컬 수정, 창고 변경은 RPC 요청.
public class NetworkStorageTransferProxy : StorageTransferService
{
    private readonly StorageSyncHandler _syncHandler;

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

        _syncHandler.RequestSwap(storageSlotIndex, inItemId, inItemCount);
    }
}