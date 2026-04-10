using UnityEngine;

/// 월드에 배치되는 창고 오브젝트. IWorldInteractable 구현.
/// PUN2 동기화는 StorageSyncHandler에 위임한다.
public class StorageObject : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private int _slotCount = 24;
    [SerializeField] private string _animationTrigger = "StorageOpen";
    [SerializeField] private ItemDatabase _itemDatabase;

    private StorageDomain _storage;
    private StorageSyncHandler _syncHandler;
    private StorageController _controller;
    private string _buildingId;
    private Vector3Int _anchor;
    private bool _isBoundToBuilding;

    public string AnimationTrigger => _animationTrigger;
    public StorageDomain Storage => _storage;
    public StorageSyncHandler SyncHandler => _syncHandler;
    public int SlotCount => _storage != null ? _storage.SlotCount : _slotCount;

    private void Awake()
    {
        _storage = new StorageDomain(_slotCount);
        _syncHandler = GetComponent<StorageSyncHandler>();
        _syncHandler?.Init(_storage, _slotCount);
    }

    private void OnEnable()
    {
        StorageController.OnReady += HandleControllerReady;

        if (StorageController.Instance != null)
            BindController(StorageController.Instance);
    }

    private void OnDisable()
    {
        StorageController.OnReady -= HandleControllerReady;
    }

    public void Interact(PlayerController player)
    {
        if (!EnsureController()) return;

        _syncHandler?.RequestFullSync();
        _controller?.OpenStorage(this);
    }

    private void OnDestroy()
    {
        StorageManager.Instance.Unregister(this);
    }

    public void BindToBuilding(string buildingId, Vector3Int anchor)
    {
        _buildingId = buildingId;
        _anchor = anchor;
        _isBoundToBuilding = true;
    }

    public bool TryGetStorageKey(out string key)
    {
        if (!_isBoundToBuilding || string.IsNullOrEmpty(_buildingId))
        {
            key = null;
            return false;
        }

        key = StorageManager.CreateKey(_buildingId, _anchor);
        return true;
    }

    public StorageSaveData ExportSaveData()
    {
        if (!_isBoundToBuilding || string.IsNullOrEmpty(_buildingId)) return null;

        var slots = _syncHandler != null
            ? _syncHandler.ExportSlots()
            : ExportSlotsDirectly();

        return new StorageSaveData
        {
            BuildingId = _buildingId,
            AnchorX = _anchor.x,
            AnchorY = _anchor.y,
            AnchorZ = _anchor.z,
            SlotCount = SlotCount,
            Slots = slots
        };
    }

    public void ImportSaveData(StorageSaveData saveData)
    {
        if (saveData == null) return;

        if (_syncHandler != null)
        {
            _syncHandler.ImportSlots(saveData.SlotCount, saveData.Slots);
        }
        else
        {
            ImportSlotsDirectly(saveData.SlotCount, saveData.Slots);
        }
    }

    private void ImportSlotsDirectly(int slotCount, System.Collections.Generic.List<InventorySlotSaveData> slots)
    {
        if (_storage == null || _itemDatabase == null) return;

        int totalSlots = Mathf.Max(slotCount, _slotCount);
        var filled = new System.Collections.Generic.List<(int index, InventorySlot slot)>();

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

        _storage.ReplaceAll(totalSlots, filled);
    }

    private System.Collections.Generic.List<InventorySlotSaveData> ExportSlotsDirectly()
    {
        var slots = new System.Collections.Generic.List<InventorySlotSaveData>();
        if (_storage == null) return slots;

        for (int i = 0; i < _storage.SlotCount; i++)
        {
            InventorySlot slot = _storage.GetSlot(i);
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

    private void HandleControllerReady(StorageController controller)
    {
        BindController(controller);
    }

    private void BindController(StorageController controller)
    {
        if (controller == null) return;
        _controller = controller;
    }

    private bool EnsureController()
    {
        if (_controller != null) return true;

        if (StorageController.Instance != null)
        {
            BindController(StorageController.Instance);
            return true;
        }

        _controller = FindFirstObjectByType<StorageController>();
        return _controller != null;
    }

    public void EndInteract() { }
}
