using System;
using UnityEngine;
using UnityEngine.UI;

public class ShopController : MonoBehaviour
{
    [Header("상점 건물")]
    [SerializeField] private Shop _testShop;

    [Header("요구 컴포넌트")]
    [SerializeField] private PlayerInventoryAbility _playerInventory;
    [SerializeField] private UI_Inventory _uiInventory;
    [SerializeField] private UI_Shop _uiShop;

    private TradeService _tradeService;
    private NpcInteractionContext _currentContext;

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

    private void OnEnable()
    {
        _uiShop.OnCloseRequested += CloseShop;
    }

    private void OnDisable()
    {
        _uiShop.OnCloseRequested -= CloseShop;
    }

    public void OpenShop(Shop shop, NpcInteractionContext context)
    {
        if (shop == null || shop.ShopData == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("열 수 있는 ShopData가 없습니다.");
#endif
            return;
        }
        _currentContext = context;
        _uiShop.Open(shop.ShopData);
        _uiInventory.SetClickMode(EInventoryClickMode.Trading);
        _playerInventory.Open();

        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseShop()
    {
        _uiShop.Close();
        _uiInventory.SetClickMode(EInventoryClickMode.Normal);
        _playerInventory.Close();

        Cursor.lockState = CursorLockMode.Locked;

        _currentContext?.InteractionComponent?.EndInteraction();
        _currentContext = null;
    }
}
