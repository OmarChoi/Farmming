using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuildResourceTracker
{
    private readonly BuildingManager _buildingManager;
    private readonly PlayerController _owner;
    private PlayerInventoryAbility _inventoryAbility;

    // ── 이벤트 바인딩 플래그 ──
    private bool _isRemoveRefundBound;
    private bool _isCostConfirmBound;
    private bool _isInventoryReadyBound;
    private bool _isSlotChangedBound;

    // ── 자원 캐시 ──
    private bool _hasResourcesCached;
    private bool _hasResourcesDirty = true;

    public bool IsDirty => _hasResourcesDirty;
    public event Action OnResourceChanged;

    public PlayerBuildResourceTracker(BuildingManager buildingManager, PlayerController owner)
    {
        _buildingManager = buildingManager;
        _owner = owner;
        CacheInventoryAbility();
    }

    public void Bind()
    {
        if (_buildingManager != null && !_isRemoveRefundBound)
        {
            _buildingManager.OnLocalRemoveRefundGranted += OnRemoveRefundGranted;
            _isRemoveRefundBound = true;
        }

        if (_buildingManager != null && !_isCostConfirmBound)
        {
            _buildingManager.OnLocalBuildCostConfirmed += OnBuildCostConfirmed;
            _isCostConfirmBound = true;
        }

        if (!_isInventoryReadyBound)
        {
            PlayerInventoryAbility.OnLocalPlayerReady += OnLocalInventoryReady;
            _isInventoryReadyBound = true;
        }

        if (_inventoryAbility != null && !_isSlotChangedBound)
        {
            _inventoryAbility.OnSlotChanged += OnSlotChanged;
            _isSlotChangedBound = true;
        }
    }

    public void Unbind()
    {
        if (_isInventoryReadyBound)
        {
            PlayerInventoryAbility.OnLocalPlayerReady -= OnLocalInventoryReady;
            _isInventoryReadyBound = false;
        }

        if (_inventoryAbility != null && _isSlotChangedBound)
        {
            _inventoryAbility.OnSlotChanged -= OnSlotChanged;
            _isSlotChangedBound = false;
        }
    }

    public void Dispose()
    {
        Unbind();

        if (_buildingManager != null && _isRemoveRefundBound)
        {
            _buildingManager.OnLocalRemoveRefundGranted -= OnRemoveRefundGranted;
            _isRemoveRefundBound = false;
        }

        if (_buildingManager != null && _isCostConfirmBound)
        {
            _buildingManager.OnLocalBuildCostConfirmed -= OnBuildCostConfirmed;
            _isCostConfirmBound = false;
        }
    }

    public void SetInventoryAbility(PlayerInventoryAbility ability)
    {
        if (ability == _inventoryAbility) return;

        if (_inventoryAbility != null && _isSlotChangedBound)
        {
            _inventoryAbility.OnSlotChanged -= OnSlotChanged;
            _isSlotChangedBound = false;
        }

        _inventoryAbility = ability;
    }

    public bool HasRequired(BuildingDataSO data)
    {
        if (data == null) return false;
        if (!EnsureInventoryAbility()) return false;

        IReadOnlyList<BuildingCostEntry> costs = data.Costs;
        foreach (BuildingCostEntry cost in costs)
        {
            if (cost.Item == null) continue;
            if (_inventoryAbility.GetItemCount(cost.Item) < cost.Amount) return false;
        }
        return true;
    }

    public bool CheckCached(BuildingDataSO data)
    {
        if (_hasResourcesDirty)
        {
            _hasResourcesCached = HasRequired(data);
            _hasResourcesDirty = false;
        }
        return _hasResourcesCached;
    }

    public int[] GetOwnedCounts(BuildingDataSO data)
    {
        if (data == null) return Array.Empty<int>();
        IReadOnlyList<BuildingCostEntry> costs = data.Costs;
        var counts = new int[costs.Count];
        if (!EnsureInventoryAbility()) return counts;

        for (int i = 0; i < costs.Count; i++)
        {
            if (costs[i].Item == null) continue;
            counts[i] = _inventoryAbility.GetItemCount(costs[i].Item);
        }
        return counts;
    }

    public void MarkDirty()
    {
        _hasResourcesDirty = true;
    }

    private bool EnsureInventoryAbility()
    {
        if (_inventoryAbility != null)
        {
            Bind();
            return true;
        }

        CacheInventoryAbility();
        if (_inventoryAbility == null) return false;

        Bind();
        return true;
    }

    private void CacheInventoryAbility()
    {
        if (_owner == null) return;
        SetInventoryAbility(_owner.GetAbility<PlayerInventoryAbility>());
    }

    private void OnRemoveRefundGranted(BuildingDataSO buildingData)
    {
        if (_owner == null || !_owner.IsMine || buildingData == null) return;
        if (!EnsureInventoryAbility()) return;

        foreach (BuildingCostEntry costItem in buildingData.Costs)
        {
            if (costItem.Item == null || costItem.Amount <= 0) continue;
            _inventoryAbility.AddItem(costItem.Item, costItem.Amount);
        }

        _hasResourcesDirty = true;
        OnResourceChanged?.Invoke();
    }

    private void OnBuildCostConfirmed(BuildingDataSO buildingData)
    {
        if (_owner == null || !_owner.IsMine || buildingData == null) return;
        if (!EnsureInventoryAbility()) return;

        foreach (BuildingCostEntry cost in buildingData.Costs)
        {
            if (cost.Item == null || cost.Amount <= 0) continue;
            _inventoryAbility.RemoveItem(cost.Item, cost.Amount);
        }

        _hasResourcesDirty = true;
        OnResourceChanged?.Invoke();
    }

    private void OnSlotChanged(int slotIndex)
    {
        if (_owner == null || !_owner.IsMine) return;

        _hasResourcesDirty = true;
        OnResourceChanged?.Invoke();
    }

    private void OnLocalInventoryReady(PlayerInventoryAbility inventoryAbility)
    {
        if (_owner == null || !_owner.IsMine || inventoryAbility == null) return;
        if (inventoryAbility.GetComponentInParent<PlayerController>() != _owner) return;

        SetInventoryAbility(inventoryAbility);
        Bind();
    }
}
