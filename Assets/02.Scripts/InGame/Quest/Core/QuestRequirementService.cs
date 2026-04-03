using System.Collections.Generic;

public class QuestRequirementService
{
    private readonly PlayerInventoryAbility _inventoryAbility;

    public QuestRequirementService(PlayerInventoryAbility inventoryAbility)
    {
        _inventoryAbility = inventoryAbility;
    }

    public bool HasRequirements(IReadOnlyList<QuestItemRequirementEntry> requirements)
    {
        if (_inventoryAbility == null || requirements == null || requirements.Count == 0) return false;

        foreach (QuestItemRequirementEntry requirement in requirements)
        {
            if (requirement.Item == null) continue;

            if (_inventoryAbility.GetItemCount(requirement.Item) < requirement.Amount)
            {
                return false;
            }
        }

        return true;
    }

    public bool TryConsumeRequirements(IReadOnlyList<QuestItemRequirementEntry> requirements)
    {
        if (!HasRequirements(requirements)) return false;

        foreach (QuestItemRequirementEntry requirement in requirements)
        {
            if (requirement.Item == null) continue;

            _inventoryAbility.RemoveItem(requirement.Item, requirement.Amount);
        }

        return true;
    }

    public int GetOwnedItemCount(ItemDataSO item)
    {
        if (_inventoryAbility == null || item == null) return 0;

        return _inventoryAbility.GetItemCount(item);
    }

    public int GetOwnedItemCount(int itemId, IReadOnlyList<QuestItemRequirementEntry> requirements)
    {
        if (_inventoryAbility == null || requirements == null || itemId < 0) return 0;

        foreach (QuestItemRequirementEntry requirement in requirements)
        {
            if (requirement.Item == null) continue;
            if (requirement.ItemId != itemId) continue;

            return _inventoryAbility.GetItemCount(requirement.Item);
        }

        return 0;
    }
}
