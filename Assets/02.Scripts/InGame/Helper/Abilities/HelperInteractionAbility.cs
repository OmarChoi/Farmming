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
        if (_owner.Energy.IsExhausted) return;
        if (_action == null) return;
        if (_playerStamina != null && !_playerStamina.Stamina.TryConsume(_owner.Data.StaminaCost)) return;

        _action.InteractPrimary(cell);
        _owner.Energy.TryConsume(_owner.Level.GetEnergyCost());
    }

    public void InteractSecondary(TerrainCell cell)
    {
        if (_owner.Energy.IsExhausted) return;
        if (_action == null) return;
        if (_playerStamina != null && !_playerStamina.Stamina.TryConsume(_owner.Data.StaminaCost)) return;

        _action.InteractSecondary(cell);
        _owner.Energy.TryConsume(_owner.Level.GetEnergyCost());
    }
}