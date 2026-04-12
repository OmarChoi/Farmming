using UnityEngine;

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

    private void Awake()
    {
        ResolveDependencies();
    }

    private void Start()
    {
        if (_uiShop == null)
        {
            enabled = false;
            return;
        }
        EnsureTradeReady();
    }
    
    private void OnEnable()
    {
        PlayerInventoryAbility.OnLocalPlayerReady += OnPlayerReady;
        if (_uiShop != null)
        {
            _uiShop.OnCloseRequested += CloseShop;
        }
    }

    private void OnDisable()
    {
        PlayerInventoryAbility.OnLocalPlayerReady -= OnPlayerReady;
        if (_uiShop != null)
        {
            _uiShop.OnCloseRequested -= CloseShop;
        }
    }

    private void OnPlayerReady(PlayerInventoryAbility ability)
    {
        _playerInventory = ability;
        InitTrade();
    }

    private void ResolveDependencies()
    {
        if (_uiShop == null)
        {
            _uiShop = FindFirstObjectByType<UI_Shop>(FindObjectsInactive.Include);
        }
        if (_uiInventory == null)
        {
            _uiInventory = FindFirstObjectByType<UI_Inventory>(FindObjectsInactive.Include);
        }
    }

    private bool EnsureTradeReady()
    {
        ResolveDependencies();

        // 현재 로컬 플레이어의 인벤토리를 다시 찾습니다.
        if (_playerInventory == null)
        {
            var inventories = FindObjectsByType<PlayerInventoryAbility>(FindObjectsSortMode.None);
            foreach (var inventory in inventories)
            {
                if (inventory == null) continue;

                var owner = inventory.GetComponentInParent<PlayerController>();
                if (owner != null && owner.IsMine)
                {
                    _playerInventory = inventory;
                    break;
                }
            }
        }

        if (_playerInventory == null || _uiShop == null || _uiInventory == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[ShopController] 거래 초기화 실패: 필요한 참조가 없습니다.");
#endif
            return false;
        }

        InitTrade();
        return true;
    }

    private void InitTrade()
    {
        if (_playerInventory == null || _uiShop == null || _uiInventory == null) return;

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
        if (!EnsureTradeReady())
        {
#if UNITY_EDITOR
            Debug.LogWarning("[ShopController] 거래 준비가 되지 않아 상점을 열 수 없습니다.");
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
}
