using Photon.Pun;
using UnityEngine;

public class HelperInteractionAbility : HelperAbility
{
    private IHelperAction _action;
    private PlayerStaminaAbility _playerStamina;

    public IHelperAction Action => _action;

    protected override void Awake()
    {
        base.Awake();
        _action = GetComponentInChildren<IHelperAction>();
    }

    public void Init()
    {
        _playerStamina = _owner.PlayerOwner?.GetAbility<PlayerStaminaAbility>();
    }

    public bool InteractPrimary(TerrainCell cell)
    {
        if (!CanInteract()) return;
        if (!_action.CanInteractPrimary(cell)) return;

        ConsumeResources();

        _action.InteractPrimary(cell);

        var pos = cell.GridPosition;
        _owner.PhotonView.RpcSafe(
            nameof(RPC_InteractPrimary), RpcTarget.Others,
            pos.x, pos.y, pos.z);
        return true;
    }

    public bool InteractSecondary(TerrainCell cell)
    {
        if (!CanInteract()) return;
        if (!_action.CanInteractSecondary(cell)) return;

        float secondaryCost = _action.GetSecondaryCost();
        if (secondaryCost >= 0)
        {
            if (!_owner.Energy.HasEnough(secondaryCost)) return false;
            _owner.Energy.TryConsume(secondaryCost);
            _playerStamina?.TryConsume(_owner.Data.StaminaCost);
        }
        else
        {
            ConsumeResources();
        }

        _action.InteractSecondary(cell);

        var pos = cell.GridPosition;
        _owner.PhotonView.RpcSafe(
            nameof(RPC_InteractSecondary), RpcTarget.Others,
            pos.x, pos.y, pos.z);
        return true;
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
        if (_action == null) return false;
        if (!_owner.Energy.HasEnough(_owner.Level.GetEnergyCost())) return false;
        if (_playerStamina != null && !_playerStamina.HasEnough(_owner.Data.StaminaCost)) return false;
        return true;
    }

    private void ConsumeResources()
    {
        _owner.Energy.TryConsume(_owner.Level.GetEnergyCost());
        _playerStamina?.TryConsume(_owner.Data.StaminaCost);
    }

    [PunRPC]
    internal void RPC_InteractPrimary(int gridX, int gridY, int gridZ)
    {
        var cell = TerrainGridManager.Instance?.GetCell(new Vector3Int(gridX, gridY, gridZ));
        if (cell == null) return;

        InteractPrimaryLocal(cell);
    }

    [PunRPC]
    internal void RPC_InteractSecondary(int gridX, int gridY, int gridZ)
    {
        var cell = TerrainGridManager.Instance?.GetCell(new Vector3Int(gridX, gridY, gridZ));
        if (cell == null) return;

        InteractSecondaryLocal(cell);
    }
}
