using System;
using Photon.Pun;
using UnityEngine;

public class RangeBoostEffect : HelperAbility
{
    public bool IsActive { get; private set; }
    public int RemainingMinutes { get; private set; }

    public event Action OnActivated;
    public event Action OnDeactivated;

    public void Activate(int durationGameMinutes = 60)
    {
        ApplyActivation(durationGameMinutes, ShouldSyncToOthers());
        _owner?.SyncRuntimeState();
    }

    private void OnMinuteChanged(GameTime time)
    {
        if (!IsActive) return;

        RemainingMinutes--;
        if (RemainingMinutes <= 0)
        {
            Deactivate(ShouldSyncToOthers());
        }
    }

    private void ApplyActivation(int durationGameMinutes, bool syncToOthers)
    {
        RemainingMinutes = durationGameMinutes;

        if (!IsActive)
        {
            IsActive = true;
            TimeEvents.OnMinuteChanged += OnMinuteChanged;
            OnActivated?.Invoke();
        }

        if (syncToOthers)
        {
            _owner?.PhotonView?.RpcSafe(
                nameof(RPC_ActivateRangeBoost),
                RpcTarget.Others,
                durationGameMinutes);
        }
    }

    private void Deactivate(bool syncToOthers)
    {
        if (!IsActive && RemainingMinutes <= 0)
            return;

        IsActive = false;
        RemainingMinutes = 0;
        TimeEvents.OnMinuteChanged -= OnMinuteChanged;
        OnDeactivated?.Invoke();

        if (syncToOthers)
        {
            _owner?.PhotonView?.RpcSafe(
                nameof(RPC_DeactivateRangeBoost),
                RpcTarget.Others);
        }

        _owner?.SyncRuntimeState();
    }

    internal void ApplySyncedState(bool isActive, int remainingMinutes)
    {
        if (isActive)
        {
            ApplyActivation(remainingMinutes, false);
            return;
        }

        Deactivate(false);
    }

    private bool ShouldSyncToOthers()
    {
        return _owner != null
               && _owner.IsMine
               && PhotonNetwork.IsConnected
               && _owner.PhotonView != null;
    }

    [PunRPC]
    internal void RPC_ActivateRangeBoost(int durationGameMinutes)
    {
        ApplyActivation(durationGameMinutes, false);
    }

    [PunRPC]
    internal void RPC_DeactivateRangeBoost()
    {
        Deactivate(false);
    }

    private void OnDestroy()
    {
        if (IsActive)
        {
            TimeEvents.OnMinuteChanged -= OnMinuteChanged;
        }
    }
}
