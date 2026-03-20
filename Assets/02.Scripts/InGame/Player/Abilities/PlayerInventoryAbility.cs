using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryAbility : PlayerAbility, ISaveableAbility
{
    [SerializeField] private KeyCode _inventoryKey = KeyCode.I;
    [SerializeField] private ItemDatabase _itemDatabase;
    [SerializeField] private CameraPreset _cameraPreset;
    private PlayerCameraAbility _cameraAbility;
    private InventoryDomain _inventory;
    private bool _isOpen;

    public bool IsOpen => _isOpen;
    public int SlotCount => _inventory.SlotCount;

    public event Action<bool> OnToggle;
    public event Action<int> OnSlotChanged
    {
        add => _inventory.OnSlotChanged += value;
        remove => _inventory.OnSlotChanged -= value;
    }
    public event Action OnInventoryResized
    {
        add => _inventory.OnInventoryResized += value;
        remove => _inventory.OnInventoryResized -= value;
    }
    
    public static event Action<PlayerInventoryAbility> OnLocalPlayerReady;

    protected override void Awake()
    {
        base.Awake();
        _inventory = new InventoryDomain();
        _cameraAbility = _owner.GetAbility<PlayerCameraAbility>();
    }

    private void Start()
    {
        // TODO: PUN2 도입 후 PhotonView.IsMine 체크 추가
        OnLocalPlayerReady?.Invoke(this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(_inventoryKey))
            Toggle();
    }

    public void Toggle()
    {
        if (_isOpen)
        {
            Close();
            _cameraAbility?.ClearPreset();
        }
        else
        {
            Open();
            _cameraAbility?.SetPreset(_cameraPreset);
        }
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        _owner.EnterUIMode();
        OnToggle?.Invoke(true);
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _owner.ExitUIMode();
        OnToggle?.Invoke(false);
    }

    public InventorySlot GetSlot(int index) => _inventory.GetSlot(index);
    public void AddItem(ItemDataSO item, int amount = 1) => _inventory.AddItem(item, amount);
    public void SwapSlots(int from, int to) => _inventory.SwapSlots(from, to);
    public void RemoveAt(int index, int amount = 1) => _inventory.RemoveAt(index, amount);

    public void ExportTo(PlayerSaveData saveData)
    {
        saveData.Inventory = new List<InventorySlotSaveData>();
        for (int i = 0; i < _inventory.SlotCount; i++)
        {
            var slot = _inventory.GetSlot(i);
            if (slot.IsEmpty) continue;
            saveData.Inventory.Add(new InventorySlotSaveData
            {
                ItemId = slot.Item.Id,
                Count = slot.Count
            });
        }
    }

    public void ImportFrom(PlayerSaveData saveData)
    {
        var slots = new List<InventorySlot>();
        foreach (var data in saveData.Inventory)
        {
            ItemDataSO item = _itemDatabase.GetById(data.ItemId);
            if (item == null) continue;

            var slot = new InventorySlot();
            slot.TryAdd(item, data.Count);
            slots.Add(slot);
        }
        _inventory.ReplaceAll(slots);
    }
}