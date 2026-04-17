using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// 창고 네트워크 동기화 전담. PUN2 RPC는 이 클래스에서만 호출한다.
/// Storage 전용 네트워크 레이어로, INetworkAdapter와 별도로 운용된다.
/// 마스터: StorageTransferService로 로컬 실행 → 전체 브로드캐스트
/// 클라이언트: RPC로 마스터에 요청 → 결과 수신
[RequireComponent(typeof(PhotonView))]
public class StorageSyncHandler : MonoBehaviourPun
{
    [SerializeField] private ItemDatabase _itemDatabase;

    private StorageDomain _storage;
    private int _slotCount;

    public bool IsMaster => !PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient;

    public void Init(StorageDomain storage, int slotCount)
    {
        _storage = storage;
        _slotCount = slotCount;
    }

    /// 창고 열 때 마스터에게 최신 상태 요청
    public void RequestFullSync()
    {
        if (IsMaster) return;
        photonView.RPC(nameof(RPC_RequestFullSync), RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber);
    }

    /// 인벤토리 → 창고: 아이템 넣기 요청. 공간 부족으로 남은 수량은 요청자에게 반환된다.
    public void RequestAddItem(int itemId, int amount)
    {
        if (IsMaster)
        {
            ExecuteAddItem(itemId, amount, PhotonNetwork.LocalPlayer?.ActorNumber ?? -1);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestAddItem), RpcTarget.MasterClient,
                itemId, amount, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    /// 창고 → 인벤토리: 아이템 빼기 요청
    public void RequestRemoveItem(int storageSlotIndex, int amount)
    {
        if (IsMaster)
        {
            ExecuteRemoveItem(storageSlotIndex, amount, PhotonNetwork.LocalPlayer?.ActorNumber ?? -1);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestRemoveItem), RpcTarget.MasterClient,
                storageSlotIndex, amount, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    /// 인벤토리 ↔ 창고 스왑 요청
    public void RequestSwap(int storageSlotIndex, int inventorySlotIndex, int inItemId, int inItemCount, bool preferInventory)
    {
        if (IsMaster)
        {
            ExecuteSwap(storageSlotIndex, inventorySlotIndex, inItemId, inItemCount, preferInventory,
                PhotonNetwork.LocalPlayer?.ActorNumber ?? -1);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestSwap), RpcTarget.MasterClient,
                storageSlotIndex, inventorySlotIndex, inItemId, inItemCount, preferInventory, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    /// 창고 내부 슬롯 스왑/이동 요청
    public void RequestSwapInStorage(int from, int to)
    {
        if (IsMaster)
        {
            _storage.SwapSlots(from, to);
            BroadcastFullSync();
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestSwapInStorage), RpcTarget.MasterClient, from, to);
        }
    }

    public void RequestSplitHalf(int storageSlotIndex)
    {
        if (IsMaster)
        {
            ExecuteSplitHalf(storageSlotIndex);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestSplitHalf), RpcTarget.MasterClient, storageSlotIndex);
        }
    }

    public void RequestPlaceSplit(int sourceIndex, int targetIndex, int itemId, int amount)
    {
        if (IsMaster)
        {
            ExecutePlaceSplit(sourceIndex, targetIndex, itemId, amount);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestPlaceSplit), RpcTarget.MasterClient,
                sourceIndex, targetIndex, itemId, amount);
        }
    }

    public void RequestAddItemToSlot(int storageSlotIndex, int itemId, int amount)
    {
        if (IsMaster)
        {
            ExecuteAddItemToSlot(storageSlotIndex, itemId, amount, PhotonNetwork.LocalPlayer?.ActorNumber ?? -1);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestAddItemToSlot), RpcTarget.MasterClient,
                storageSlotIndex, itemId, amount, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    public void RequestGiveHeldItem(int itemId, int amount, int inventorySlotIndex = -1)
    {
        if (IsMaster)
        {
            GiveItemToPlayer(PhotonNetwork.LocalPlayer?.ActorNumber ?? -1, itemId, amount, inventorySlotIndex);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestGiveHeldItem), RpcTarget.MasterClient,
                itemId, amount, PhotonNetwork.LocalPlayer.ActorNumber, inventorySlotIndex);
        }
    }

    /// 지갑 → 창고 입금 요청. 마스터가 창고에 가산한 뒤 RPC_SpendGold로 요청자 지갑을 차감한다.
    public void RequestDepositGold(int amount)
    {
        if (IsMaster)
        {
            ExecuteDepositGold(amount, PhotonNetwork.LocalPlayer?.ActorNumber ?? -1);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestDepositGold), RpcTarget.MasterClient,
                amount, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    /// 창고 → 지갑 출금 요청. 마스터가 잔액 검증 후 지급한다.
    public void RequestWithdrawGold(int amount)
    {
        if (IsMaster)
        {
            ExecuteWithdrawGold(amount, PhotonNetwork.LocalPlayer?.ActorNumber ?? -1);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestWithdrawGold), RpcTarget.MasterClient,
                amount, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    // === 마스터 실행 로직 ===

    private void ExecuteAddItem(int itemId, int amount, int actorNumber)
    {
        var item = _itemDatabase.GetById(itemId);
        if (item == null)
        {
            // 아이템을 조회하지 못하면 요청자에게 원 수량 그대로 반환 (데이터 소실 방지)
            GiveItemToPlayer(actorNumber, itemId, amount);
            return;
        }

        int remaining = _storage.AddItemAndGetRemaining(item, amount);
        if (remaining > 0)
            GiveItemToPlayer(actorNumber, itemId, remaining);

        BroadcastFullSync();
    }

    private void ExecuteRemoveItem(int storageSlotIndex, int amount, int actorNumber)
    {
        var slot = _storage.GetSlot(storageSlotIndex);
        if (slot == null || slot.IsEmpty) return;

        int toRemove = Mathf.Min(amount, slot.Count);
        int itemId = slot.Item.Id;
        _storage.RemoveAt(storageSlotIndex, toRemove);

        // 요청자에게 아이템 지급
        GiveItemToPlayer(actorNumber, itemId, toRemove);
        BroadcastFullSync();
    }

    private void ExecuteSwap(int storageSlotIndex, int inventorySlotIndex, int inItemId, int inItemCount, bool preferInventory, int actorNumber)
    {
        var stoSlot = _storage.GetSlot(storageSlotIndex);
        if (stoSlot == null) return;

        // 같은 아이템 + 인벤토리로 합치기 희망 → 창고의 잔량을 인벤토리 슬롯에 합치기
        if (preferInventory && !stoSlot.IsEmpty && inItemId > 0 && inItemId == stoSlot.Item.Id && inItemCount > 0)
        {
            int total = stoSlot.Count + inItemCount;
            int maxStack = stoSlot.Item.MaxStack;
            int toInventory = Mathf.Min(total, maxStack);
            int remainder = total - toInventory;

            int mergedItemId = stoSlot.Item.Id;
            stoSlot.Clear();
            if (remainder > 0)
            {
                var mergedItem = _itemDatabase.GetById(mergedItemId);
                if (mergedItem != null) stoSlot.TryAdd(mergedItem, remainder);
            }

            _storage.NotifySlotChanged(storageSlotIndex);

            if (toInventory > 0)
                GiveItemToPlayer(actorNumber, mergedItemId, toInventory, inventorySlotIndex);

            BroadcastFullSync();
            return;
        }

        // 창고 슬롯의 기존 아이템 기록
        int outItemId = 0;
        int outItemCount = 0;
        if (!stoSlot.IsEmpty)
        {
            outItemId = stoSlot.Item.Id;
            outItemCount = stoSlot.Count;
            stoSlot.Clear();
        }

        // 플레이어가 보낸 아이템을 슬롯에 넣기
        if (inItemId > 0 && inItemCount > 0)
        {
            var inItem = _itemDatabase.GetById(inItemId);
            if (inItem != null)
                stoSlot.TryAdd(inItem, inItemCount);
        }

        // 슬롯 변경 알림
        _storage.NotifySlotChanged(storageSlotIndex);

        // 꺼낸 아이템을 요청자에게 지급
        if (outItemId > 0 && outItemCount > 0)
            GiveItemToPlayer(actorNumber, outItemId, outItemCount, inventorySlotIndex);

        BroadcastFullSync();
    }

    private void ExecuteAddItemToSlot(int storageSlotIndex, int itemId, int amount, int actorNumber)
    {
        var item = _itemDatabase.GetById(itemId);
        if (item == null)
        {
            GiveItemToPlayer(actorNumber, itemId, amount);
            return;
        }

        bool placed = _storage.AddItemToSlot(item, storageSlotIndex, amount, fallbackToAuto: false);
        if (!placed)
        {
            // 지정 슬롯에 못 넣으면 요청자에게 반환 (창고 자동 배치 대신 사용자 의도 유지)
            GiveItemToPlayer(actorNumber, itemId, amount);
        }
        BroadcastFullSync();
    }

    private void ExecuteSplitHalf(int storageSlotIndex)
    {
        if (_storage.SplitHalf(storageSlotIndex) > 0)
            BroadcastFullSync();
    }

    private void ExecutePlaceSplit(int sourceIndex, int targetIndex, int itemId, int amount)
    {
        if (amount <= 0) return;

        var item = _itemDatabase.GetById(itemId);
        if (item == null) return;

        _storage.PlaceSplit(sourceIndex, targetIndex, item, amount);
        BroadcastFullSync();
    }

    private void ExecuteDepositGold(int amount, int actorNumber)
    {
        if (amount <= 0) return;
        _storage.AddGold(amount);
        BroadcastFullSync();
        DeductGoldFromPlayer(actorNumber, amount);
    }

    private void ExecuteWithdrawGold(int amount, int actorNumber)
    {
        if (amount <= 0) return;
        if (!_storage.TryRemoveGold(amount)) return;

        GiveGoldToPlayer(actorNumber, amount);
        BroadcastFullSync();
    }

    private void GiveGoldToPlayer(int actorNumber, int amount)
    {
        if (PhotonNetwork.IsConnected && actorNumber >= 0)
        {
            var target = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (target != null)
            {
                if (target.IsLocal)
                    CurrencyManager.Instance?.AddGold(amount);
                else
                    photonView.RPC(nameof(RPC_ReceiveGold), target, amount);
            }
        }
        else
        {
            CurrencyManager.Instance?.AddGold(amount);
        }
    }

    private void DeductGoldFromPlayer(int actorNumber, int amount)
    {
        if (PhotonNetwork.IsConnected && actorNumber >= 0)
        {
            var target = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (target != null)
            {
                if (target.IsLocal)
                    CurrencyManager.Instance?.TrySpendGold(amount);
                else
                    photonView.RPC(nameof(RPC_SpendGold), target, amount);
            }
        }
        else
        {
            CurrencyManager.Instance?.TrySpendGold(amount);
        }
    }

    private void GiveItemToPlayer(int actorNumber, int itemId, int amount, int inventorySlotIndex = -1)
    {
        if (PhotonNetwork.IsConnected && actorNumber >= 0)
        {
            var target = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (target != null)
            {
                if (target.IsLocal)
                    ReceiveItem(itemId, amount, inventorySlotIndex);
                else
                    photonView.RPC(nameof(RPC_ReceiveItem), target, itemId, amount, inventorySlotIndex);
            }
        }
        else
        {
            ReceiveItem(itemId, amount, inventorySlotIndex);
        }
    }

    private void ReceiveItem(int itemId, int amount, int inventorySlotIndex = -1)
    {
        var item = _itemDatabase.GetById(itemId);
        if (item == null) return;

        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (!pc.IsMine) continue;
            var inventory = pc.GetAbility<PlayerInventoryAbility>();
            if (inventory == null) break;

            if (inventorySlotIndex >= 0)
                inventory.AddItemToSlot(item, inventorySlotIndex, amount, fallbackToAuto: true);
            else
                inventory.AddItem(item, amount);
            break;
        }
    }

    // === 동기화 ===

    public void BroadcastFullSync()
    {
        string json = Serialize();

        if (PhotonNetwork.IsConnected)
            photonView.RPC(nameof(RPC_SyncStorage), RpcTarget.Others, json);
    }

    private string Serialize()
    {
        var data = new StorageSyncData
        {
            Slots = ExportSlots(),
            Gold = _storage.Gold
        };
        return JsonUtility.ToJson(data);
    }

    private void Deserialize(string json)
    {
        var data = JsonUtility.FromJson<StorageSyncData>(json);
        ImportSlots(_slotCount, data?.Slots);
        _storage.SetGold(data?.Gold ?? 0);
    }

    public List<InventorySlotSaveData> ExportSlots()
    {
        var slots = new List<InventorySlotSaveData>();
        for (int i = 0; i < _storage.SlotCount; i++)
        {
            var slot = _storage.GetSlot(i);
            if (slot == null || slot.IsEmpty) continue;

            slots.Add(new InventorySlotSaveData
            {
                SlotIndex = i,
                ItemId = slot.Item.Id,
                Count = slot.Count
            });
        }

        return slots;
    }

    public void ImportSlots(int slotCount, List<InventorySlotSaveData> slots)
    {
        int totalSlots = Mathf.Max(slotCount, _slotCount);
        var filled = new List<(int index, InventorySlot slot)>();

        if (slots != null)
        {
            foreach (var entry in slots)
            {
                var item = _itemDatabase.GetById(entry.ItemId);
                if (item == null) continue;

                var slot = new InventorySlot();
                slot.TryAdd(item, entry.Count);
                filled.Add((entry.SlotIndex, slot));
            }
        }

        _slotCount = totalSlots;
        _storage.ReplaceAll(totalSlots, filled);
    }

    // === RPC ===

    [PunRPC]
    private void RPC_RequestFullSync(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        string json = Serialize();
        var target = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (target != null)
            photonView.RPC(nameof(RPC_SyncStorage), target, json);
    }

    [PunRPC]
    private void RPC_SyncStorage(string json)
    {
        Deserialize(json);
    }

    [PunRPC]
    private void RPC_RequestAddItem(int itemId, int amount, int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ExecuteAddItem(itemId, amount, actorNumber);
    }

    [PunRPC]
    private void RPC_RequestRemoveItem(int storageSlotIndex, int amount, int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ExecuteRemoveItem(storageSlotIndex, amount, actorNumber);
    }

    [PunRPC]
    private void RPC_RequestSwap(int storageSlotIndex, int inventorySlotIndex, int inItemId, int inItemCount, bool preferInventory, int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ExecuteSwap(storageSlotIndex, inventorySlotIndex, inItemId, inItemCount, preferInventory, actorNumber);
    }

    [PunRPC]
    private void RPC_RequestSwapInStorage(int from, int to)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        _storage.SwapSlots(from, to);
        BroadcastFullSync();
    }

    [PunRPC]
    private void RPC_RequestSplitHalf(int storageSlotIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ExecuteSplitHalf(storageSlotIndex);
    }

    [PunRPC]
    private void RPC_RequestPlaceSplit(int sourceIndex, int targetIndex, int itemId, int amount)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ExecutePlaceSplit(sourceIndex, targetIndex, itemId, amount);
    }

    [PunRPC]
    private void RPC_RequestAddItemToSlot(int storageSlotIndex, int itemId, int amount, int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ExecuteAddItemToSlot(storageSlotIndex, itemId, amount, actorNumber);
    }

    [PunRPC]
    private void RPC_RequestGiveHeldItem(int itemId, int amount, int actorNumber, int inventorySlotIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        GiveItemToPlayer(actorNumber, itemId, amount, inventorySlotIndex);
    }

    [PunRPC]
    private void RPC_ReceiveItem(int itemId, int amount, int inventorySlotIndex)
    {
        ReceiveItem(itemId, amount, inventorySlotIndex);
    }

    [PunRPC]
    private void RPC_RequestDepositGold(int amount, int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ExecuteDepositGold(amount, actorNumber);
    }

    [PunRPC]
    private void RPC_RequestWithdrawGold(int amount, int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ExecuteWithdrawGold(amount, actorNumber);
    }

    [PunRPC]
    private void RPC_ReceiveGold(int amount)
    {
        CurrencyManager.Instance?.AddGold(amount);
    }

    [PunRPC]
    private void RPC_SpendGold(int amount)
    {
        CurrencyManager.Instance?.TrySpendGold(amount);
    }

    // === 네트워크 동기화 DTO ===

    [System.Serializable]
    private class StorageSyncData
    {
        public List<InventorySlotSaveData> Slots;
        public int Gold;
    }
}