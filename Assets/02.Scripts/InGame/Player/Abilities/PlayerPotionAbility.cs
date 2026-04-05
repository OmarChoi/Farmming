using UnityEngine;

public class PlayerPotionAbility : PlayerAbility
{
    private PlayerHelperInteractionAbility _helperInteraction;
    private PlayerInventoryAbility _inventoryAbility;
    private int _usePotionCount = 1;

    protected override void Awake()
    {
        base.Awake();
        _helperInteraction = _owner.GetAbility<PlayerHelperInteractionAbility>();
        _inventoryAbility = _owner.GetAbility<PlayerInventoryAbility>();
    }

    public bool TryUsePotion(int slotIndex)
    {
        if(_inventoryAbility == null || _helperInteraction == null) return false;

        var slot = _inventoryAbility .GetSlot(slotIndex);
        if(slot == null || slot.IsEmpty) return false;

        if(!(slot.Item is PotionDataSO potion)) return false;

        var helper = _helperInteraction.CurrentHelper;
        if(helper == null) return false;

        ApplyPotion(potion, helper);

        _inventoryAbility.RemoveAt(slotIndex, _usePotionCount);

        return true;
    }

    private void ApplyPotion(PotionDataSO potion, HelperController helper)
    {
        switch(potion.PotionType)
        {
            case EPotionType.RangeBoost:
                var rangeBoost = helper.GetAbility<RangeBoostEffect>();
                if(rangeBoost != null)
                {
                    rangeBoost.Activate(potion.DurationGameMinutes);
                }
                break;
        }
    }
}
