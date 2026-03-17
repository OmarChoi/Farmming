using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopData", menuName = "Scriptable Objects/Shop/ShopData")]
public class ShopData : ScriptableObject
{
    [field: SerializeField] public string ShopName { get; private set; }
    [field: SerializeField] public List<ItemDataSO> SellItems { get; private set; }
}
