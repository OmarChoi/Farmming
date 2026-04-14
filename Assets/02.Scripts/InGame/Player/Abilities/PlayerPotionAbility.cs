using UnityEngine;

public class PlayerPotionAbility : PlayerAbility
{
    private PlayerHelperInteractionAbility _helperInteraction;
    private PlayerHelperInventoryAbility _helperInventoryAbility;
    private PlayerInventoryAbility _inventoryAbility;
    private PlayerStaminaAbility _playerStaminaAbility;
    private int _usePotionCount = 1;
    [SerializeField] private float _playerStaminaHealRate = 0.3f;

    protected override void Awake()
    {
        base.Awake();
        _helperInteraction = _owner.GetAbility<PlayerHelperInteractionAbility>();
        _helperInventoryAbility = _owner.GetAbility<PlayerHelperInventoryAbility>();
        _inventoryAbility = _owner.GetAbility<PlayerInventoryAbility>();
        _playerStaminaAbility = _owner.GetAbility<PlayerStaminaAbility>();
    }

    public bool TryUsePotion(int slotIndex)
    {
        if(_inventoryAbility == null) return false;

        var slot = _inventoryAbility .GetSlot(slotIndex);
        if(slot == null || slot.IsEmpty) return false;

        if(!(slot.Item is PotionDataSO potion)) return false;

        if (!TryApplyPotion(potion))
            return false;

        _inventoryAbility.RemoveAt(slotIndex, _usePotionCount);

        return true;
    }

    private bool TryApplyPotion(PotionDataSO potion)
    {
        switch(potion.PotionType)
        {
            case EPotionType.RangeBoost:
                var helper = _helperInteraction != null ? _helperInteraction.CurrentHelper : null;
                if(helper == null) return false;

                var rangeBoost = helper.GetAbility<RangeBoostEffect>();
                if(rangeBoost != null)
                {
                    rangeBoost.Activate(potion.DurationGameMinutes);
                    return true;
                }
                break;
            case EPotionType.HelperStaminaHeal:
                HelperController activeMainHelper = _helperInventoryAbility != null ? _helperInventoryAbility.ActiveMainHelper : null;
                if (activeMainHelper == null)
                    return false;
                if (activeMainHelper.Data != null && activeMainHelper.Data.IsLightHelper)
                    return false;

                activeMainHelper.Energy.RecoverFull();
                activeMainHelper.SyncRuntimeState();
                return true;
            case EPotionType.PlayerStaminaHeal:
                if (_playerStaminaAbility == null || _playerStaminaAbility.Stamina == null)
                    return false;
                if (_playerStaminaAbility.Stamina.Current >= _playerStaminaAbility.Stamina.Max)
                    return false;

                float recoverAmount = _playerStaminaAbility.Stamina.Max * _playerStaminaHealRate;
                return _playerStaminaAbility.TryRecover(recoverAmount);
        }

        return false;
    }
}
