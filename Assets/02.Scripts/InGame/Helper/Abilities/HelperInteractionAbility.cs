using UnityEngine;

public class HelperInteractionAbility : HelperAbility
{
    private IHelperAction _action;
    private PlayerStaminaAbility _playerStamina;

    private void Start()
    {
        _action = _owner.GetComponentInChildren<IHelperAction>();
        _playerStamina = _owner.PlayerOwner?.GetAbility<PlayerStaminaAbility>();
    }

    public void InteractPrimary(TerrainCell cell)
    {
        if (!CanInteract()) return;

        _action.InteractPrimary(cell);
        _owner.Energy.TryConsume(_owner.Level.GetEnergyCost());
    }

    public void InteractSecondary(TerrainCell cell)
    {
        if (!CanInteract()) return;

        _action.InteractSecondary(cell);
        _owner.Energy.TryConsume(_owner.Level.GetEnergyCost());
    }

    private bool CanInteract()
    {
        if (_owner.Energy.IsExhausted) return false;
        if (_action == null) return false;
        if (_playerStamina != null && !_playerStamina.TryConsume(_owner.Data.StaminaCost)) return false;
        return true;
    }
}