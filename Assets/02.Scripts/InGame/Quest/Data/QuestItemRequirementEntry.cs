using System;
using UnityEngine;

[Serializable]
public struct QuestItemRequirementEntry
{
    [SerializeField] private ItemDataSO _item;
    [SerializeField] private int _amount;

    public ItemDataSO Item => _item;
    public int Amount => _amount < 1 ? 1 : _amount;

    public int ItemId => _item != null ? _item.Id : -1;
    public string ItemName => _item != null ? _item.DisplayName : string.Empty;

    public QuestItemRequirementEntry WithAmount(int amount)
    {
        return new QuestItemRequirementEntry
        {
            _item = _item,
            _amount = amount
        };
    }
}
