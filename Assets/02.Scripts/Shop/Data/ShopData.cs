using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopData", menuName = "Scriptable Objects/Shop/ShopData")]
public class ShopData : ScriptableObject
{
    public string ShopName;
    public List<ItemDataSO> SellItems;
}
