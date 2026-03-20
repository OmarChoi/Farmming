using UnityEngine;

public class Shop : MonoBehaviour
{
    [SerializeField] private ShopData _shopData;

    public ShopData ShopData => _shopData;
}
