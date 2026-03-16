using System;
using UnityEngine;

[Serializable]
public struct DropEntry
{
    [SerializeField] private ItemSO _item;
    [SerializeField] private int _minQuantity;
    [SerializeField] private int _maxQuantity;

    public ItemSO Item => _item;
    public int MinQuantity => _minQuantity;
    public int MaxQuantity => _maxQuantity;

    public int GetRandomQuantity()
    {
        return UnityEngine.Random.Range(_minQuantity, _maxQuantity + 1);
    }
}
