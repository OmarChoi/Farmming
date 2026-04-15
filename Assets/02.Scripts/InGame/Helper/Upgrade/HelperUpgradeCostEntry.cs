using UnityEngine;
using System;

[Serializable]
public class HelperUpgradeCostEntry
{
    [SerializeField] private ItemDataSO _item;
    [SerializeField] private int _amount;

    public ItemDataSO Item => _item;
    public int Amount => _amount;
}
