using System;
using System.Collections.Generic;
using UnityEngine;

public class GroundSelectAbility : HelperAbility
{
    public event Action<ItemDataSO> OnGroundSelected;

    private List<(int slotIndex, ItemDataSO item)> _availableGrounds = new();
    private int _selectedIndex = -1;
    private PlayerInventoryAbility _inventory;

    public ItemDataSO SelectedGround =>
        _selectedIndex >= 0 && _selectedIndex < _availableGrounds.Count
            ? _availableGrounds[_selectedIndex].item
            : null;

    public int SelectedGroundSlotIndex =>
        _selectedIndex >= 0 && _selectedIndex < _availableGrounds.Count
            ? _availableGrounds[_selectedIndex].slotIndex
            : -1;

    private void Start()
    {
        _inventory = _owner.PlayerOwner?.GetAbility<PlayerInventoryAbility>();
        if (_inventory != null)
        {
            _inventory.OnSlotChanged += OnInventoryChanged;
        }

        RefreshGrounds();
    }

    private void OnDestroy()
    {
        if (_inventory != null)
        {
            _inventory.OnSlotChanged -= OnInventoryChanged;
        }
    }

    private void OnInventoryChanged(int _)
    {
        RefreshGrounds();
    }

    private void Update()
    {
        if (_availableGrounds.Count == 0) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll < 0f)
            SelectNext();
        else if (scroll > 0f)
            SelectPrev();
    }

    private void SelectNext()
    {
        if (_availableGrounds.Count == 0) return;
        _selectedIndex = (_selectedIndex + 1) % _availableGrounds.Count;
        OnGroundSelected?.Invoke(SelectedGround);
    }

    private void SelectPrev()
    {
        if (_availableGrounds.Count == 0) return;
        _selectedIndex = (_selectedIndex - 1 + _availableGrounds.Count) % _availableGrounds.Count;
        OnGroundSelected?.Invoke(SelectedGround);
    }

    private void RefreshGrounds()
    {
        if (_inventory == null) return;

        _availableGrounds.Clear();
        for (int i = 0; i < _inventory.SlotCount; i++)
        {
            InventorySlot slot = _inventory.GetSlot(i);
            if (slot == null || slot.IsEmpty) continue;

            if (slot.Item.IsGround)
            {
                _availableGrounds.Add((i, slot.Item));
            }
        }

        if (_selectedIndex >= _availableGrounds.Count)
            _selectedIndex = _availableGrounds.Count - 1;

        if (_selectedIndex < 0 && _availableGrounds.Count > 0)
            _selectedIndex = 0;

        OnGroundSelected?.Invoke(SelectedGround);
    }
}
