using System;
using UnityEngine;
using System.Collections.Generic;

public class SeedSelectAbility : HelperAbility
{
    public event Action<SeedItemDataSO> OnSeedSelected;

    private readonly HashSet<SeedItemDataSO> _availableSeeds = new();
    private SeedItemDataSO _selectedSeed;
    private PlayerInventoryAbility _inventory;

    public SeedItemDataSO SelectedSeed => _selectedSeed;
    public int SelectedSeedCount => GetSeedCount(_selectedSeed);
    public bool HasSelectedSeedAvailable => _selectedSeed != null && SelectedSeedCount > 0;

    private void Start()
    {
        _inventory = _owner.PlayerOwner?.GetAbility<PlayerInventoryAbility>();
        if (_inventory != null)
        {
            _inventory.OnSlotChanged += OnInventoryChanged;
        }

        RefreshSeeds();
    }

    private void OnDestroy()
    {
        if (_inventory != null)
        {
            _inventory.OnSlotChanged -= OnInventoryChanged;
        }
    }

    private void OnInventoryChanged(int index)
    {
        RefreshSeeds();
    }

    public bool TrySelectSeed(SeedItemDataSO seed)
    {
        if (seed == null || _inventory == null)
            return false;

        if (!_availableSeeds.Contains(seed) || GetSeedCount(seed) <= 0)
            return false;

        _selectedSeed = seed;
        NotifySelectionChanged();
        return true;
    }

    public void ClearSelection()
    {
        if (_selectedSeed == null)
            return;

        _selectedSeed = null;
        NotifySelectionChanged();
    }

    private void RefreshSeeds()
    {
        if (_inventory == null)
        {
            _selectedSeed = null;
            NotifySelectionChanged();
            return;
        }

        _availableSeeds.Clear();

        for (int i = 0; i < _inventory.SlotCount; i++)
        {
            InventorySlot slot = _inventory.GetSlot(i);
            if (slot.IsEmpty)
            {
                continue;
            }

            if (slot.Item.Type == EItemType.Seed && slot.Item is SeedItemDataSO seedItem)
            {
                _availableSeeds.Add(seedItem);
            }
        }

        NotifySelectionChanged();
    }

    private int GetSeedCount(SeedItemDataSO seed)
    {
        if (seed == null || _inventory == null)
            return 0;

        return _inventory.GetItemCount(seed);
    }

    private void NotifySelectionChanged()
    {
        OnSeedSelected?.Invoke(_selectedSeed);
    }
}
