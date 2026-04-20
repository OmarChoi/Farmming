using System;
using UnityEngine;
using System.Collections.Generic;

public class SeedSelectAbility : HelperAbility
{
    public event Action<SeedItemDataSO> OnSeedSelected;

    private readonly HashSet<SeedItemDataSO> _availableSeeds = new();
    private SeedItemDataSO _selectedSeed;
    private PlayerInventoryAbility _inventory;
    private PlayerController _boundPlayerOwner;

    public SeedItemDataSO SelectedSeed => _selectedSeed;
    public int SelectedSeedCount
    {
        get
        {
            EnsureInventoryBinding();
            return GetSeedCount(_selectedSeed);
        }
    }
    public bool HasSelectedSeedAvailable => HasValidSeed();
    public bool HasAnySeedAvailable
    {
        get
        {
            EnsureInventoryBinding();
            return FindFirstAvailableSeed() != null;
        }
    }

    private void Start()
    {
        EnsureInventoryBinding();
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
        EnsureInventoryBinding();

        if (seed == null || _inventory == null)
            return false;

        if (!IsSeedAvailable(seed))
            return false;

        SetSelectedSeed(seed);
        return true;
    }

    public bool TryAutoSelectSeed()
    {
        EnsureInventoryBinding();

        if (HasValidSeed())
            return true;
        if (_selectedSeed != null)
            return false;

        return TrySelectFirstAvailableSeed();
    }

    public bool TrySwitchNextSeed()
    {
        EnsureInventoryBinding();

        if (HasValidSeed())
        {
            NotifySelectionChanged();
            return true;
        }

        SeedItemDataSO seed = FindFirstAvailableSeed();
        SetSelectedSeed(seed);
        return seed != null;
    }

    public bool HasValidSeed()
    {
        EnsureInventoryBinding();
        return IsSeedAvailable(_selectedSeed);
    }

    public void ClearSelection()
    {
        if (_selectedSeed == null)
            return;

        SetSelectedSeed(null);
    }

    private void RefreshSeeds()
    {
        EnsureInventoryBinding();

        if (_inventory == null)
        {
            SetSelectedSeed(null);
            return;
        }

        _availableSeeds.Clear();

        for (int i = 0; i < _inventory.SlotCount; i++)
        {
            InventorySlot slot = _inventory.GetSlot(i);
            if (slot == null || slot.IsEmpty)
                continue;

            if (IsSeedItem(slot.Item) && GetSeedCount((SeedItemDataSO)slot.Item) > 0)
            {
                SeedItemDataSO seedItem = (SeedItemDataSO)slot.Item;
                _availableSeeds.Add(seedItem);
            }
        }

        if (_selectedSeed != null && !IsSeedAvailable(_selectedSeed))
        {
            TrySwitchNextSeed();
            return;
        }

        NotifySelectionChanged();
    }

    private int GetSeedCount(SeedItemDataSO seed)
    {
        if (seed == null || _inventory == null)
            return 0;

        return _inventory.GetItemCount(seed);
    }

    private bool TrySelectFirstAvailableSeed(int minimumAmount = 1)
    {
        SeedItemDataSO seed = FindFirstAvailableSeed(minimumAmount);
        if (seed == null)
            return false;

        SetSelectedSeed(seed);
        return true;
    }

    private bool IsSeedAvailable(SeedItemDataSO seed, int minimumAmount = 1)
    {
        if (seed == null || _inventory == null || !IsSeedItem(seed))
            return false;

        return GetSeedCount(seed) >= minimumAmount;
    }

    private SeedItemDataSO FindFirstAvailableSeed(int minimumAmount = 1)
    {
        if (_inventory == null)
            return null;

        for (int i = 0; i < _inventory.SlotCount; i++)
        {
            InventorySlot slot = _inventory.GetSlot(i);
            if (slot == null || slot.IsEmpty)
                continue;

            if (IsSeedItem(slot.Item) && GetSeedCount((SeedItemDataSO)slot.Item) >= minimumAmount)
                return (SeedItemDataSO)slot.Item;
        }

        return null;
    }

    public static bool IsSeedItem(ItemDataSO item)
    {
        return item != null && item.Type == EItemType.Seed && item is SeedItemDataSO;
    }

    private void SetSelectedSeed(SeedItemDataSO seed)
    {
        if (_selectedSeed == seed)
        {
            NotifySelectionChanged();
            return;
        }

        _selectedSeed = seed;
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
        OnSeedSelected?.Invoke(_selectedSeed);
    }
}
