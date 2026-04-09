using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// 창고 네트워크 동기화 전담. PUN2 RPC는 이 클래스에서만 호출한다.
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

    /// 인벤토리 → 창고: 아이템 넣기 요청
    public void RequestAddItem(int itemId, int amount)
    {
        if (IsMaster)
        {
            ExecuteAddItem(itemId, amount);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestAddItem), RpcTarget.MasterClient, itemId, amount);
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
    public void RequestSwap(int storageSlotIndex, int inItemId, int inItemCount)
    {
        if (IsMaster)
        {
            ExecuteSwap(storageSlotIndex, inItemId, inItemCount,
                PhotonNetwork.LocalPlayer?.ActorNumber ?? -1);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestSwap), RpcTarget.MasterClient,
                storageSlotIndex, inItemId, inItemCount, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    // === 마스터 실행 로직 ===

    private void ExecuteAddItem(int itemId, int amount)
    {
        var item = _itemDatabase.GetById(itemId);
        if (item == null) return;

        _storage.AddItem(item, amount);
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

    private void ExecuteSwap(int storageSlotIndex, int inItemId, int inItemCount, int actorNumber)
    {
        var stoSlot = _storage.GetSlot(storageSlotIndex);

        // 창고 슬롯의 기존 아이템 기록
        int outItemId = 0;
        int outItemCount = 0;
        if (stoSlot != null && !stoSlot.IsEmpty)
        {
            outItemId = stoSlot.Item.Id;
            outItemCount = stoSlot.Count;
            stoSlot.Clear();
        }

        // 플레이어가 보낸 아이템을 슬롯에 넣기
        if (inItemId > 0 && inItemCount > 0)
        {
            var inItem = _itemDatabase.GetById(inItemId);
            if (inItem != null && stoSlot != null)
                stoSlot.TryAdd(inItem, inItemCount);
        }

        // 슬롯 변경 알림
        _storage.RemoveAt(storageSlotIndex, 0);

        // 꺼낸 아이템을 요청자에게 지급
        if (outItemId > 0 && outItemCount > 0)
            GiveItemToPlayer(actorNumber, outItemId, outItemCount);

        BroadcastFullSync();
    }

    private void GiveItemToPlayer(int actorNumber, int itemId, int amount)
    {
        if (PhotonNetwork.IsConnected && actorNumber >= 0)
        {
            var target = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (target != null)
            {
                if (target.IsLocal)
                    ReceiveItem(itemId, amount);
                else
                    photonView.RPC(nameof(RPC_ReceiveItem), target, itemId, amount);
            }
        }
        else
        {
            ReceiveItem(itemId, amount);
        }
    }

    private void ReceiveItem(int itemId, int amount)
    {
        var item = _itemDatabase.GetById(itemId);
        if (item == null) return;

        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (!pc.IsMine) continue;
            pc.GetAbility<PlayerInventoryAbility>()?.AddItem(item, amount);
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
        var data = new StorageSyncData { Slots = new List<StorageSlotEntry>() };
        for (int i = 0; i < _storage.SlotCount; i++)
        {
            var slot = _storage.GetSlot(i);
            if (slot.IsEmpty) continue;
            data.Slots.Add(new StorageSlotEntry
            {
                SlotIndex = i,
                ItemId = slot.Item.Id,
                Count = slot.Count
            });
        }
        return JsonUtility.ToJson(data);
    }

    private void Deserialize(string json)
    {
        var data = JsonUtility.FromJson<StorageSyncData>(json);
        var filled = new List<(int index, InventorySlot slot)>();

        foreach (var entry in data.Slots)
        {
            var item = _itemDatabase.GetById(entry.ItemId);
            if (item == null) continue;

            var slot = new InventorySlot();
            slot.TryAdd(item, entry.Count);
            filled.Add((entry.SlotIndex, slot));
        }

        _storage.ReplaceAll(_slotCount, filled);
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
    private void RPC_RequestAddItem(int itemId, int amount)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ExecuteAddItem(itemId, amount);
    }

    [PunRPC]
    private void RPC_RequestRemoveItem(int storageSlotIndex, int amount, int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ExecuteRemoveItem(storageSlotIndex, amount, actorNumber);
    }

    [PunRPC]
    private void RPC_RequestSwap(int storageSlotIndex, int inItemId, int inItemCount, int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ExecuteSwap(storageSlotIndex, inItemId, inItemCount, actorNumber);
    }

    [PunRPC]
    private void RPC_ReceiveItem(int itemId, int amount)
    {
        ReceiveItem(itemId, amount);
    }

    // === DTO ===

    [System.Serializable]
    private class StorageSyncData
    {
        public List<StorageSlotEntry> Slots;
    }

    [System.Serializable]
    private class StorageSlotEntry
    {
        public int SlotIndex;
        public int ItemId;
        public int Count;
    }
}