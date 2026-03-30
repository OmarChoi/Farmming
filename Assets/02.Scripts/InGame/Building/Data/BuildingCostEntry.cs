using System;
using UnityEngine;

// 건물 건설에 필요한 자원 항목
[Serializable]
public struct BuildingCostEntry
{
    [SerializeField] private ItemDataSO _item;
    [SerializeField] private int _amount;

    public ItemDataSO Item => _item;
    public int Amount => _amount;
}
