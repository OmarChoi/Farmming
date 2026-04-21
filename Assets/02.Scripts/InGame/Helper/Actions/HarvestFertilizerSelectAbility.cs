using System;
using System.Collections.Generic;
using UnityEngine;

public class HarvestFertilizerSelectAbility : HelperAbility
{
    public event Action<ItemDataSO> OnFertilizerSelected;

    private readonly HashSet<ItemDataSO> _availableFertilizers = new();
    private ItemDataSO _selectedFertilizer;
    private PlayerInventoryAbility _inventory;
    private PlayerController _boundPlayerOwner;
    private HelperController _helperController;

    public ItemDataSO SelectedFertilizer => _selectedFertilizer;
    public int SelectedFertilizerCount
    {
        get
        {
            EnsureInventoryBinding();
            return GetFertilizerCount(_selectedFertilizer);
        }
    }
    public bool HasSelectedFertilizerAvailable => HasValidFertilizer();
    public bool HasAnyFertilizerAvailable
    {
        get
        {
            EnsureInventoryBinding();
            return FindFirstAvailableFertilizer() != null;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        _helperController = GetComponent<HelperController>() ?? GetComponentInParent<HelperController>();
    }

    private void Start()
    {
        EnsureInventoryBinding();
        RefreshFertilizers();
    }

    private void OnDestroy()
    {
        if (_inventory != null)
            _inventory.OnSlotChanged -= OnInventoryChanged;
    }

    private void LateUpdate()
    {
        if (_helperController != null
            && _helperController.State != EHelperState.Equipped
            && _selectedFertilizer != null)
        {
            ClearSelection();
        }
    }

    private void OnInventoryChanged(int _)
    {
        RefreshFertilizers();
    }

    public bool TrySelectFertilizer(ItemDataSO fertilizerItem)
    {
        EnsureInventoryBinding();

        if (fertilizerItem == null || _inventory == null)
            return false;

        if (!IsFertilizerAvailable(fertilizerItem))
            return false;

        SetSelectedFertilizer(fertilizerItem);
        return true;
    }

    public bool TryAutoSelectFertilizer()
    {
        EnsureInventoryBinding();

        if (HasValidFertilizer())
            return true;
        if (_selectedFertilizer != null)
            return false;

        return TrySelectFirstAvailableFertilizer();
    }

    public bool TrySwitchNextFertilizer()
    {
        EnsureInventoryBinding();

        if (HasValidFertilizer())
        {
            NotifySelectionChanged();
            return true;
        }

        ItemDataSO fertilizerItem = FindFirstAvailableFertilizer();
        SetSelectedFertilizer(fertilizerItem);
        return fertilizerItem != null;
    }

    public bool HasValidFertilizer()
    {
        EnsureInventoryBinding();
        return IsFertilizerAvailable(_selectedFertilizer);
    }

    public void ClearSelection()
    {
        if (_selectedFertilizer == null)
            return;

        SetSelectedFertilizer(null);
    }

    private void RefreshFertilizers()
    {
        EnsureInventoryBinding();

        if (_inventory == null)
        {
            SetSelectedFertilizer(null);
            return;
        }

        _availableFertilizers.Clear();
        for (int i = 0; i < _inventory.SlotCount; i++)
        {
            InventorySlot slot = _inventory.GetSlot(i);
            if (slot == null || slot.IsEmpty || !IsFertilizer(slot.Item))
                continue;

            if (GetFertilizerCount(slot.Item) > 0)
                _availableFertilizers.Add(slot.Item);
        }

        if (_selectedFertilizer != null && !IsFertilizerAvailable(_selectedFertilizer))
        {
            TrySwitchNextFertilizer();
            return;
        }

        NotifySelectionChanged();
    }

    private int GetFertilizerCount(ItemDataSO fertilizerItem)
    {
        if (fertilizerItem == null || _inventory == null)
            return 0;

        return _inventory.GetItemCount(fertilizerItem);
    }

    private bool TrySelectFirstAvailableFertilizer(int minimumAmount = 1)
    {
        ItemDataSO fertilizerItem = FindFirstAvailableFertilizer(minimumAmount);
        if (fertilizerItem == null)
            return false;

        SetSelectedFertilizer(fertilizerItem);
        return true;
    }

    private bool IsFertilizerAvailable(ItemDataSO fertilizerItem, int minimumAmount = 1)
    {
        if (fertilizerItem == null || _inventory == null || !IsFertilizer(fertilizerItem))
            return false;

        return GetFertilizerCount(fertilizerItem) >= minimumAmount;
    }

    private ItemDataSO FindFirstAvailableFertilizer(int minimumAmount = 1)
    {
        if (_inventory == null)
            return null;

        for (int i = 0; i < _inventory.SlotCount; i++)
        {
            InventorySlot slot = _inventory.GetSlot(i);
            if (slot == null || slot.IsEmpty)
                continue;

            if (IsFertilizer(slot.Item) && GetFertilizerCount(slot.Item) >= minimumAmount)
                return slot.Item;
        }

        return null;
    }

    private static bool IsFertilizer(ItemDataSO item)
    {
        return item != null && item.Type == EItemType.Fertilizer;
    }

    private void SetSelectedFertilizer(ItemDataSO fertilizerItem)
    {
        if (_selectedFertilizer == fertilizerItem)
        {
            NotifySelectionChanged();
            return;
        }

        _selectedFertilizer = fertilizerItem;
        NotifySelectionChanged();
    }

    private void EnsureInventoryBinding()
    {
        PlayerController playerOwner = _owner.PlayerOwner;
        if (_boundPlayerOwner == playerOwner && (_inventory != null || playerOwner == null))
            return;

        PlayerInventoryAbility inventory = playerOwner?.GetAbility<PlayerInventoryAbility>();
        if (_inventory == inventory)
        {
            _boundPlayerOwner = playerOwner;
            return;
        }

        if (_inventory != null)
            _inventory.OnSlotChanged -= OnInventoryChanged;

        _boundPlayerOwner = playerOwner;
        _inventory = inventory;

        if (_inventory != null)
            _inventory.OnSlotChanged += OnInventoryChanged;
    }

    private void NotifySelectionChanged()
    {
        OnFertilizerSelected?.Invoke(_selectedFertilizer);
    }
}
