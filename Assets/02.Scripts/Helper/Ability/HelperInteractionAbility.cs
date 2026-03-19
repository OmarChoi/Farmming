using UnityEngine;

public class HelperInteractionAbility : HelperAbility
{
    private IHelperAction _action;

    private void Start()
    {
        _action = _owner.GetComponentInChildren<IHelperAction>();
    }

    public void InteractPrimary(TerrainCell cell)
    {
        if (_owner.Energy.IsExhausted) return;
        if (_action == null) return;

        _action.InteractPrimary(cell);
        _owner.Energy.TryConsume(_owner.Level.GetEnergyCost());
    }

    public void InteractSecondary(TerrainCell cell)
    {
        if (_owner.Energy.IsExhausted) return;
        if (_action == null) return;

        _action.InteractSecondary(cell);
        _owner.Energy.TryConsume(_owner.Level.GetEnergyCost());
    }
}