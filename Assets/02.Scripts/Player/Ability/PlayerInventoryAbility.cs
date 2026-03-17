using System;
using UnityEngine;

public class PlayerInventoryAbility : PlayerAbility
{
    [SerializeField] private KeyCode _inventoryKey = KeyCode.I;
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

    /// 로컬 플레이어의 InventoryAbility가 생성되면 발생.
    /// UI_Inventory가 이 이벤트를 구독하여 바인딩한다.
    public static event Action<PlayerInventoryAbility> OnLocalPlayerReady;

    protected override void Awake()
    {
        base.Awake();
        _inventory = new InventoryDomain();
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
        if (_isOpen) Close();
        else Open();
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
        saveData.Inventory = _inventory.ExportSlots();
    }

    public void ImportFrom(PlayerSaveData saveData, ItemDatabase itemDb)
    {
        _inventory.ImportSlots(saveData.Inventory, itemDb);
    }
}