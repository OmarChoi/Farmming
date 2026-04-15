using UnityEngine;

public class ShopController : MonoBehaviour
{
    [Header("상점 건물")]
    [SerializeField] private Shop _shopPrefab;

    [Header("요구 컴포넌트")]
    [SerializeField] private UI_Inventory _uiInventory;
    [SerializeField] private UI_Shop _uiShop;

    private PlayerController _currentPlayer;
    private PlayerInventoryAbility _currentInventory;
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
        _uiShop.CloseImmediate();
    }
    
    private void OnEnable()
    {
        if (_uiShop != null)
        {
            _uiShop.OnCloseRequested += CloseShop;
        }
    }

    private void OnDisable()
    {
        if (_uiShop != null)
        {
            _uiShop.OnCloseRequested -= CloseShop;
        }
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

        if (context == null || context.Interactor == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[ShopController] 상호작용 플레이어 정보가 없습니다.");
#endif
            return;
        }

        ResolveDependencies();

        if (_uiShop == null || _uiInventory == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[ShopController] UI 참조가 없습니다.");
#endif
            return;
        }

        if (!TryBindPlayer(context.Interactor))
        {
#if UNITY_EDITOR
            Debug.LogWarning("[ShopController] 플레이어 인벤토리를 찾을 수 없습니다.");
#endif
            return;
        }

        _currentContext = context;

        _uiShop.Open(shop.ShopData);
        _uiInventory.SetClickMode(EInventoryClickMode.Trading);
        _currentInventory.Open();
    }

    public void CloseShop()
    {
        _uiShop.Close();
        _uiInventory.SetClickMode(EInventoryClickMode.Normal);
        _currentInventory?.Close();

        _currentContext?.InteractionComponent?.EndInteraction();
        _currentContext = null;
        _currentPlayer = null;
        _currentInventory = null;
        _tradeService = null;
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

    private bool TryBindPlayer(PlayerController player)
    {
        if (player == null) return false;

        var inventory = player.GetAbility<PlayerInventoryAbility>();
        if (inventory == null) return false;

        _currentPlayer = player;
        _currentInventory = inventory;
        _tradeService = new TradeService(_currentInventory);

        _uiShop.Init(_tradeService);
        _uiInventory.Init(_tradeService);

        return true;
    }
}
