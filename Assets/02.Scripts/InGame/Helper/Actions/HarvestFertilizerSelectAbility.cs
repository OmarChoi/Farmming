using System;
using System.Collections.Generic;
using UnityEngine;

public class HarvestFertilizerSelectAbility : HelperAbility
{
    public event Action<ItemDataSO> OnFertilizerSelected;

    private readonly HashSet<ItemDataSO> _availableFertilizers = new();
    private ItemDataSO _selectedFertilizer;
    private PlayerInventoryAbility _inventory;
    private HelperController _helperController;

    public ItemDataSO SelectedFertilizer => _selectedFertilizer;
    public int SelectedFertilizerCount => GetFertilizerCount(_selectedFertilizer);
    public bool HasSelectedFertilizerAvailable => _selectedFertilizer != null && SelectedFertilizerCount > 0;

    protected override void Awake()
    {
        base.Awake();
        _helperController = GetComponent<HelperController>() ?? GetComponentInParent<HelperController>();
    }

    private void Start()
    {
        _inventory = _owner.PlayerOwner?.GetAbility<PlayerInventoryAbility>();
        if (_inventory != null)
            _inventory.OnSlotChanged += OnInventoryChanged;

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
        if (fertilizerItem == null || _inventory == null)
            return false;

        if (!_availableFertilizers.Contains(fertilizerItem))
            return false;

        if (GetFertilizerCount(fertilizerItem) <= 0)
            return false;

        _selectedFertilizer = fertilizerItem;
        NotifySelectionChanged();
        return true;
    }

    public void ClearSelection()
    {
        if (_selectedFertilizer == null)
            return;

        _selectedFertilizer = null;
        NotifySelectionChanged();
    }

    private void RefreshFertilizers()
    {
        if (_inventory == null)
        {
            _selectedFertilizer = null;
            NotifySelectionChanged();
            return;
        }

        _availableFertilizers.Clear();
        for (int i = 0; i < _inventory.SlotCount; i++)
        {
            InventorySlot slot = _inventory.GetSlot(i);
            if (slot == null || slot.IsEmpty || !IsFertilizer(slot.Item))
                continue;

            _availableFertilizers.Add(slot.Item);
        }

        if (_selectedFertilizer != null
            && (!_availableFertilizers.Contains(_selectedFertilizer) || GetFertilizerCount(_selectedFertilizer) <= 0))
        {
            _selectedFertilizer = null;
        }

        NotifySelectionChanged();
    }

    private int GetFertilizerCount(ItemDataSO fertilizerItem)
    {
        if (fertilizerItem == null || _inventory == null)
            return 0;

        return _inventory.GetItemCount(fertilizerItem);
    }

    private static bool IsFertilizer(ItemDataSO item)
    {
        return item != null && item.Type == EItemType.Fertilizer;
    }

    private void NotifySelectionChanged()
    {
        OnFertilizerSelected?.Invoke(_selectedFertilizer);
    }
}
