using System;
using UnityEngine;

[Serializable]
public struct DropEntry
{
    [SerializeField] private ItemDataSO _item;
    [SerializeField] private int _minQuantity;
    [SerializeField] private int _maxQuantity;

    public ItemDataSO Item => _item;
    public int MinQuantity => _minQuantity;
    public int MaxQuantity => _maxQuantity;

    public int GetRandomQuantity()
    {
        if (_minQuantity > _maxQuantity)
        {
            (_minQuantity, _maxQuantity) = (_maxQuantity, _minQuantity);
        }
        return UnityEngine.Random.Range(_minQuantity, _maxQuantity + 1);
    }
}
