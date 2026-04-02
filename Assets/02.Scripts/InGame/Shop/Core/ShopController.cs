using System;
using UnityEngine;
using UnityEngine.UI;

public class ShopController : MonoBehaviour
{
    [Header("상점 건물")]
    [SerializeField] private Shop _testShop;

    [Header("요구 컴포넌트")]
    [SerializeField] private UI_Inventory _uiInventory;
    [SerializeField] private UI_Shop _uiShop;

    private PlayerInventoryAbility _playerInventory;
    private TradeService _tradeService;
    private NpcInteractionContext _currentContext;
    
    private void Start()
    {
        if (_uiShop == null)
        {
            enabled = false;
            return;
        }
    }
    
    private void OnEnable()
    {
        PlayerInventoryAbility.OnLocalPlayerReady += OnPlayerReady;
        _uiShop.OnCloseRequested += CloseShop;
    }

    private void OnDisable()
    {
        PlayerInventoryAbility.OnLocalPlayerReady -= OnPlayerReady;
        _uiShop.OnCloseRequested -= CloseShop;
    }

    private void Init()
    {
        _tradeService = new TradeService(_playerInventory);
        _uiShop.Init(_tradeService);
        _uiInventory.Init(_tradeService);
        _uiShop.Close();
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
    }

    public void CloseShop()
    {
        _uiShop.Close();
        _uiInventory.SetClickMode(EInventoryClickMode.Normal);
        _playerInventory.Close();

        _currentContext?.InteractionComponent?.EndInteraction();
        _currentContext = null;
    }
    
    
    private void OnPlayerReady(PlayerInventoryAbility ability)
    {
        Debug.Log("PlayerInventoryAbility 확인");
        _playerInventory = ability;
        Init();
    }
}
