using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Data/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemDataSO> _items;
    private Dictionary<int, ItemDataSO> _itemDictionary;
    public IList<ItemDataSO> Items => _items;
    public ItemDataSO GetById(int itemId)
    {
        EnsureDictionary();
        _itemDictionary.TryGetValue(itemId, out ItemDataSO item);
        return item;
    }

    private void EnsureDictionary()
    {
        if (_itemDictionary != null && _itemDictionary.Count > 0) return;

        _itemDictionary = new Dictionary<int, ItemDataSO>();
        foreach (var item in _items)
        {
            if (item != null)
                _itemDictionary[item.Id] = item;
        }
    }
}