using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// 창고/금고 UI 최상위 코디네이터.
/// 실제 기능은 UI_StorageItemPanel(창고), UI_StorageGoldPanel(금고)가 담당.
public class UI_Storage : MonoBehaviour
{
    [Header("루트")]
    [SerializeField] private GameObject _panel;

    [Header("닫기 버튼 (각 패널의 X 버튼 모두 등록)")]
    [SerializeField] private Button[] _exitButtons;

    [Header("탭 (책갈피)")]
    [SerializeField] private Button _storageTabButton;
    [SerializeField] private Button _goldTabButton;
    [SerializeField] private RectTransform _storageTabRect; // StoragePanel 전체 RectTransform
    [SerializeField] private RectTransform _goldTabRect;    // VaultPanel 전체 RectTransform
    [SerializeField] private float _tabSwitchDuration = 0.25f;
    [SerializeField] private float _tabPopScale = 1.03f; // 전환 시 살짝 커졌다가 복귀

    [Header("하위 패널")]
    [SerializeField] private UI_StorageItemPanel _itemPanel;
    [SerializeField] private UI_StorageGoldPanel _goldPanel;

    [Header("슬라이드 애니메이션")]
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private float _slideDistance = 300f;

    private RectTransform _panelRect;
    private Vector2 _panelOriginPos;
    private Tween _slideTween;

    private Tween _storagePanelTween;
    private Tween _goldPanelTween;

    private enum ETabType { Storage, Gold }
    private ETabType _currentETab = ETabType.Storage;

    private StorageDomain _storage;
    private StorageTransferService _transferService;
    private StorageController _boundController;

    // UI_Inventory의 크로스드래그가 참조하는 호버 슬롯 pass-through
    public UI_Slot HoveredSlot => _itemPanel != null ? _itemPanel.HoveredSlot : null;
    public bool IsDragging => _itemPanel != null && _itemPanel.IsDragging;
    public UI_Slot GetSlotUnderPointer()
    {
        EnsureBoundToCurrentSession();
        return _itemPanel != null ? _itemPanel.GetSlotUnderPointer() : null;
    }

    private void Awake()
    {
        StorageUiRegistry.RegisterStorage(this);
        StorageController.OnReady += HandleControllerReady;
        StorageUiRegistry.OnInventoryRegistered += HandleInventoryRegistered;
        StorageUiRegistry.OnInventoryUnregistered += HandleInventoryUnregistered;

        if (StorageController.Instance != null)
            BindController(StorageController.Instance);

        if (StorageUiRegistry.CurrentInventory != null)
            HandleInventoryRegistered(StorageUiRegistry.CurrentInventory);

        _panelRect = _panel.GetComponent<RectTransform>();
        _panelOriginPos = _panelRect.anchoredPosition;
        _panel.SetActive(false);

        if (_exitButtons != null)
        {
            foreach (var btn in _exitButtons)
            {
                if (btn != null)
                    btn.onClick.AddListener(HandleCloseRequested);
            }
        }

        if (_storageTabButton != null)
            _storageTabButton.onClick.AddListener(() => SelectTab(ETabType.Storage));
        if (_goldTabButton != null)
            _goldTabButton.onClick.AddListener(() => SelectTab(ETabType.Gold));

        ApplyTabImmediate(ETabType.Storage);
    }

    private void OnDestroy()
    {
        if (_exitButtons != null)
        {
            foreach (var btn in _exitButtons)
            {
                if (btn != null)
                    btn.onClick.RemoveListener(HandleCloseRequested);
            }
        }
        if (_storageTabButton != null)
            _storageTabButton.onClick.RemoveAllListeners();
        if (_goldTabButton != null)
            _goldTabButton.onClick.RemoveAllListeners();

        _storagePanelTween?.Kill();
        _goldPanelTween?.Kill();

        StorageUiRegistry.UnregisterStorage(this);
        StorageController.OnReady -= HandleControllerReady;
        StorageUiRegistry.OnInventoryRegistered -= HandleInventoryRegistered;
        StorageUiRegistry.OnInventoryUnregistered -= HandleInventoryUnregistered;
        UnbindController();
    }

    public void Open()
    {
        _slideTween?.Kill();
        _panel.SetActive(true);

        _panelRect.anchoredPosition = _panelOriginPos + Vector2.left * _slideDistance;
        _slideTween = _panelRect.DOAnchorPos(_panelOriginPos, _slideDuration)
            .SetEase(Ease.OutBack)
            .SetLink(_panel);
    }

    public void Close()
    {
        _slideTween?.Kill();
        _itemPanel?.CancelDrag();
        _itemPanel?.HideTooltip();

        Vector2 target = _panelOriginPos + Vector2.left * _slideDistance;
        _slideTween = _panelRect.DOAnchorPos(target, _slideDuration)
            .SetEase(Ease.InBack)
            .SetLink(_panel)
            .OnComplete(() => _panel.SetActive(false));
    }

    // === 탭 전환 ===

    private void ApplyTabImmediate(ETabType eTab)
    {
        _currentETab = eTab;
        bool isStorage = eTab == ETabType.Storage;

        // 두 패널 모두 항상 활성. 같은 위치에 겹쳐 있고, 활성 탭이 최상단으로 옴.
        if (_storageTabRect != null)
        {
            _storageTabRect.gameObject.SetActive(true);
            _storageTabRect.localScale = Vector3.one;
        }
        if (_goldTabRect != null)
        {
            _goldTabRect.gameObject.SetActive(true);
            _goldTabRect.localScale = Vector3.one;
        }

        BringToFront(isStorage ? _storageTabRect : _goldTabRect);
    }

    private void SelectTab(ETabType eTab)
    {
        if (_currentETab == eTab) return;
        _currentETab = eTab;
        bool isStorage = eTab == ETabType.Storage;

        RectTransform showing = isStorage ? _storageTabRect : _goldTabRect;

        if (isStorage)
            _goldPanel?.Unbind();

        _storagePanelTween?.Kill();
        _goldPanelTween?.Kill();

        if (showing != null)
        {
            BringToFront(showing);
            showing.localScale = Vector3.one * _tabPopScale;
            var popTween = showing.DOScale(Vector3.one, _tabSwitchDuration)
                .SetEase(Ease.OutBack)
                .SetLink(showing.gameObject);

            if (isStorage) _storagePanelTween = popTween;
            else _goldPanelTween = popTween;
        }

        if (!isStorage && _goldPanel != null && _storage != null && _transferService != null)
            _goldPanel.Bind(_storage, _transferService);
    }

    private void BringToFront(RectTransform target)
    {
        if (target == null) return;
        target.SetAsLastSibling();
    }

    // === StorageController 바인딩 ===

    private void HandleControllerReady(StorageController controller)
    {
        BindController(controller);
    }

    private void BindController(StorageController controller)
    {
        if (controller == null) return;
        if (_boundController == controller) return;

        UnbindController();

        _boundController = controller;
        _boundController.OnStorageOpened += HandleStorageOpened;
        _boundController.OnStorageClosed += HandleStorageClosed;

        if (controller.IsOpen && controller.CurrentSession != null)
            HandleStorageOpened(controller.CurrentSession);
    }

    private void UnbindController()
    {
        if (_boundController == null) return;

        _boundController.OnStorageOpened -= HandleStorageOpened;
        _boundController.OnStorageClosed -= HandleStorageClosed;
        _boundController = null;
    }

    private void HandleStorageOpened(StorageSession session)
    {
        if (session == null) return;

        _storage = session.Storage;
        _transferService = session.TransferService;

        _itemPanel?.Init(_storage, _transferService);
        _itemPanel?.SetLinkedInventory(ResolveCurrentInventory());

        Open();
    }

    public bool EnsureBoundToCurrentSession()
    {
        StorageController controller = StorageController.Instance;
        if (controller == null || !controller.IsOpen || controller.CurrentSession == null)
            return false;

        BindController(controller);

        StorageSession session = controller.CurrentSession;
        if (_storage != session.Storage || _transferService != session.TransferService)
        {
            HandleStorageOpened(session);
        }
        else
        {
            _itemPanel?.SetLinkedInventory(ResolveCurrentInventory());
        }

        return true;
    }

    private void HandleStorageClosed()
    {
        _itemPanel?.SetLinkedInventory(null);
        _itemPanel?.Unbind();
        _goldPanel?.Unbind();
        ApplyTabImmediate(ETabType.Storage);
        _storage = null;
        _transferService = null;
        Close();
    }

    private static void HandleCloseRequested()
    {
        StorageController.Instance?.CloseStorage();
    }

    private void HandleInventoryRegistered(UI_Inventory inventory)
    {
        if (_boundController == null || !_boundController.IsOpen) return;
        _itemPanel?.SetLinkedInventory(inventory);
    }

    private void HandleInventoryUnregistered()
    {
        _itemPanel?.SetLinkedInventory(null);
    }

    private static UI_Inventory ResolveCurrentInventory()
    {
        return StorageUiRegistry.CurrentInventory != null
            ? StorageUiRegistry.CurrentInventory
            : FindFirstObjectByType<UI_Inventory>(FindObjectsInactive.Include);
    }
}
