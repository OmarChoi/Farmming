using UnityEngine;

public class ShopController : MonoBehaviour
{
    [SerializeField] private Shop _testShop;

    [SerializeField] private PlayerInventoryAbility _playerInventory;
    [SerializeField] private UI_Inventory _uiInventory;
    [SerializeField] private UI_Shop _uiShop;

    private TradeService _tradeService;

    private void Start()
    {
        if (_playerInventory == null || _uiShop == null)
        {
#if UNITY_EDITOR
            Debug.LogError("ShopController 참조가 비어 있습니다.");
#endif
            enabled = false;
            return;
        }

        _tradeService = new TradeService(_playerInventory);
        _uiShop.Init(_tradeService);
        _uiInventory.Init(_tradeService);
        _uiShop.Close();
    }

    // 테스트용 상점 데이터입니다. (실제 게임에서는 NPC와 상호작용할 때 해당 NPC의 ShopData를 사용합니다.)
    private void Update()
    {
#if UNITY_EDITOR

        if (Input.GetKey(KeyCode.Escape))
        {
            CloseShop();
        }
#endif
    }

    public void OpenShop(Shop shop)
    {
        if (shop == null || shop.ShopData == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("열 수 있는 ShopData가 없습니다.");
#endif
            return;
        }

        _uiShop.Open(shop.ShopData);
        _uiInventory.SetClickMode(EInventoryClickMode.Trading);
        _playerInventory.Open();
    }

    public void CloseShop()
    {
        _uiShop.Close();
        _uiInventory.SetClickMode(EInventoryClickMode.Normal);
        _playerInventory.Close();
    }
}
