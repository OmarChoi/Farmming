using System;
using UnityEngine;
using System.Collections.Generic;

public class SeedSelectAbility : HelperAbility
{
    public event Action<SeedConfig> OnSeedSelected;

    private List<SeedConfig> _availableSeeds = new();
    private int _selectedIndex = -1;

    public SeedConfig SelectedSeed => _selectedIndex >= 0 && _selectedIndex < _availableSeeds.Count ? _availableSeeds[_selectedIndex] : null;

    private PlayerInventoryAbility GetInventory()
    {
        return _owner.PlayerOwner?.GetAbility<PlayerInventoryAbility>();
    }

    private void Start()
    {
        PlayerInventoryAbility inventory = GetInventory();
        if (inventory != null)
        {
            inventory.OnSlotChanged += OnInventoryChanged;
        }

        RefreshSeeds();
    }

    private void OnDestroy()
    {
        PlayerInventoryAbility inventory = GetInventory();
        if (inventory != null)
        {
            inventory.OnSlotChanged -= OnInventoryChanged;
        }
    }

    private void OnInventoryChanged(int index)
    {
        RefreshSeeds();
    }

    private void Update()
    {
        if (_availableSeeds.Count == 0)
        {
            return;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll < 0f)
        {
            SelectNext();
        }
        else if (scroll > 0f)
        {
            SelectPrev();
        }
    }

    private void SelectNext()
    {
        if (_availableSeeds.Count == 0)
        {
            return;
        }
        _selectedIndex = (_selectedIndex + 1) % _availableSeeds.Count;
        OnSeedSelected?.Invoke(SelectedSeed);
    }

    private void SelectPrev()
    {
        if (_availableSeeds.Count == 0)
        {
            return;
        }
        _selectedIndex = (_selectedIndex - 1 + _availableSeeds.Count) % _availableSeeds.Count;
        OnSeedSelected?.Invoke(SelectedSeed);
    }

    private void RefreshSeeds()
    {
        PlayerInventoryAbility inventory = GetInventory();
        if (inventory == null)
        {
            return;
        }

        _availableSeeds.Clear();

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            InventorySlot slot = inventory.GetSlot(i);
            if (slot.IsEmpty)
            {
                continue;
            }

            if (slot.Item.Type == EItemType.Seed && slot.Item is SeedItemDataSO seedItem)
            {
                _availableSeeds.Add(seedItem.SeedConfig);
            }
        }

        if (_selectedIndex >= _availableSeeds.Count)
        {
            _selectedIndex = _availableSeeds.Count - 1;
        }

        if (_selectedIndex < 0 && _availableSeeds.Count > 0)
        {
            _selectedIndex = 0;
        }

        OnSeedSelected?.Invoke(SelectedSeed);
    }
}
