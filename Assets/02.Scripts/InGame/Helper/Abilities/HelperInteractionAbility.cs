using Photon.Pun;
using UnityEngine;

public class HelperInteractionAbility : HelperAbility
{
    private IHelperAction _action;
    private PlayerStaminaAbility _playerStamina;

    protected override void Awake()
    {
        base.Awake();
        _action = _owner.GetComponentInChildren<IHelperAction>();
    }

    public void Init()
    {
        _playerStamina = _owner.PlayerOwner?.GetAbility<PlayerStaminaAbility>();
    }

    public void InteractPrimary(TerrainCell cell)
    {
        if (!CanInteract()) return;

        _action.InteractPrimary(cell);
        _owner.Energy.TryConsume(_owner.Level.GetEnergyCost());

        var pos = cell.GridPosition;
        _owner.PhotonView?.RPC(
            nameof(HelperController.RPC_InteractPrimary), RpcTarget.Others,
            pos.x, pos.y, pos.z);
    }

    public void InteractSecondary(TerrainCell cell)
    {
        if (!CanInteract()) return;

        _action.InteractSecondary(cell);
        _owner.Energy.TryConsume(_owner.Level.GetEnergyCost());

        var pos = cell.GridPosition;
        _owner.PhotonView?.RPC(
            nameof(HelperController.RPC_InteractSecondary), RpcTarget.Others,
            pos.x, pos.y, pos.z);
    }

    public void InteractPrimaryLocal(TerrainCell cell)
    {
        _action?.InteractPrimary(cell);
    }

    public void InteractSecondaryLocal(TerrainCell cell)
    {
        _action?.InteractSecondary(cell);
    }

    private bool CanInteract()
    {
        if (_owner.IsActing) return false;
        if (_owner.Energy.IsExhausted) return false;
        if (_action == null) return false;
        if (_playerStamina != null && !_playerStamina.TryConsume(_owner.Data.StaminaCost)) return false;
        return true;
    }
}