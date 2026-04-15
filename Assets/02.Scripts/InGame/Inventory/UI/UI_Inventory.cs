using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Inventory : MonoBehaviour, ISlotContainer
{
    private const int Columns = 4;
    public static event Func<SeedItemDataSO, bool> SeedSelectionRequested;
    public static event Func<ItemDataSO, bool> GroundSelectionRequested;
    public static event Func<ItemDataSO, bool> FertilizerSelectionRequested;

    [Header("참조")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Transform _slotContainer;
    [SerializeField] private UI_Slot _uiSlotPrefab;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private UI_ItemTooltip _tooltip;
    [SerializeField] private Image _dragIcon;
    [SerializeField] private UI_TradeAmountPopup _tradeAmountPopup;

    [Header("슬라이드 애니메이션")]
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private float _slideDistance = 300f;

    private RectTransform _panelRect;
    private Vector2 _panelOriginPos;
    private Tween _slideTween;
    private PlayerInventoryAbility _inventoryAbility;
    private readonly List<UI_Slot> _slotUIs = new();

    private TradeService _tradeService;
    private StorageTransferService _storageTransferService;
    private StorageController _boundStorageController;
    private UI_Storage _linkedStorage;
    private EInventoryClickMode _clickMode = EInventoryClickMode.Normal;

    // 드래그 상태
    private bool _isDragging;
    private UI_Slot _dragSourceSlot;
    private UI_Slot _hoveredSlot;
    private Transform _dragIconOriginalParent;

    public UI_Slot HoveredSlot => _hoveredSlot;

    // 분할 드래그 상태
    private bool _isSplitDrag;
    private ItemDataSO _splitItem;
    private int _splitAmount;

    private PlayerPotionAbility _potionAbility;

    private void Awake()
    {
        StorageUiRegistry.RegisterInventory(this);
        StorageUiRegistry.OnStorageRegistered += HandleStorageRegistered;
        StorageUiRegistry.OnStorageUnregistered += HandleStorageUnregistered;

        _panelRect = _panel.GetComponent<RectTransform>();
        _panelOriginPos = _panelRect.anchoredPosition;
        _panel.SetActive(false);
        _dragIcon.gameObject.SetActive(false);
        PlayerInventoryAbility.OnLocalPlayerReady += Bind;
        TryBindExistingPlayer();
        StorageController.OnReady += HandleStorageControllerReady;

        if (StorageController.Instance != null)
            BindStorageController(StorageController.Instance);

        if (StorageUiRegistry.CurrentStorage != null)
            HandleStorageRegistered(StorageUiRegistry.CurrentStorage);
    }

    private void OnDestroy()
    {
        PlayerInventoryAbility.OnLocalPlayerReady -= Bind;
        StorageController.OnReady -= HandleStorageControllerReady;
        StorageUiRegistry.OnStorageRegistered -= HandleStorageRegistered;
        StorageUiRegistry.OnStorageUnregistered -= HandleStorageUnregistered;
        StorageUiRegistry.UnregisterInventory(this);
        UnbindStorageController();
        Unbind();
    }

    private void Update()
    {
        if (!_isDragging) return;

        _dragIcon.transform.position = Input.mousePosition;

        if (!Input.GetMouseButton(0))
            EndDrag();
    }

    private void TryBindExistingPlayer()
    {
        if (_inventoryAbility != null) return;

        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player == null || !player.IsMine) continue;
            var ability = player.GetAbility<PlayerInventoryAbility>();
            if (ability == null) continue;
            Bind(ability);
            return;
        }
    }

    private void Bind(PlayerInventoryAbility ability)
    {
        Unbind();

        _inventoryAbility = ability;
        _potionAbility = ability.GetComponent<PlayerPotionAbility>();
        _inventoryAbility.OnToggle += OnToggle;
        _inventoryAbility.OnSlotChanged += RefreshSlot;
        _inventoryAbility.OnInventoryResized += SyncSlotCount;

        SyncSlotCount();
        RefreshAll();
    }

    private void Unbind()
    {
        if (_inventoryAbility == null) return;

        _inventoryAbility.OnToggle -= OnToggle;
        _inventoryAbility.OnSlotChanged -= RefreshSlot;
        _inventoryAbility.OnInventoryResized -= SyncSlotCount;
        _inventoryAbility = null;
        _potionAbility = null;
    }

    private void OnToggle(bool open)
    {
        _slideTween?.Kill();
        CancelDrag();

        if (open)
        {
            _panel.SetActive(true);
            RefreshAll();
            _panelRect.anchoredPosition = _panelOriginPos + Vector2.right * _slideDistance;
            _slideTween = _panelRect.DOAnchorPos(_panelOriginPos, _slideDuration)
                .SetEase(Ease.OutBack)
                .SetLink(_panel);
        }
        else
        {
            if (_tooltip != null)
                _tooltip.Hide();

            Vector2 target = _panelOriginPos + Vector2.right * _slideDistance;
            _slideTween = _panelRect.DOAnchorPos(target, _slideDuration)
                .SetEase(Ease.InBack)
                .SetLink(_panel)
                .OnComplete(() => _panel.SetActive(false));
        }
    }

    // 슬롯 UI 동기화

    private void SyncSlotCount()
    {
        int target = _inventoryAbility.SlotCount;

        for (int i = _slotUIs.Count; i < target; i++)
        {
            var slotUI = Instantiate(_uiSlotPrefab, _slotContainer);
            slotUI.Init(this, i);
            _slotUIs.Add(slotUI);
        }

        while (_slotUIs.Count > target)
        {
            int last = _slotUIs.Count - 1;
            Destroy(_slotUIs[last].gameObject);
            _slotUIs.RemoveAt(last);
        }
    }

    // 갱신

    private void RefreshAll()
    {
        for (int i = 0; i < _slotUIs.Count; i++)
            RefreshSlot(i);
    }

    private void RefreshSlot(int index)
    {
        if (index < 0 || index >= _slotUIs.Count) return;
        _slotUIs[index].Refresh(_inventoryAbility.GetSlot(index));
    }

    // 툴팁

    public void OnSlotHoverEnter(UI_Slot slot)
    {
        _hoveredSlot = slot;

        if (_isDragging) return;
        if (_tooltip == null) return;

        if (slot.CurrentItem == null)
        {
            _tooltip.Hide();
            return;
        }

        int column = slot.SlotIndex % Columns;
        bool showRight = column < 2;
        _tooltip.Show(slot.CurrentItem, slot.RectTransform, showRight);
    }

    public void OnSlotHoverExit()
    {
        _hoveredSlot = null;

        if (!_isDragging && _tooltip != null)
            _tooltip.Hide();
    }

    // 드래그 앤 드롭

    public void BeginDrag(UI_Slot source, bool shift = false)
    {
        if (_clickMode == EInventoryClickMode.Trading) return;
        if (source.CurrentItem == null) return;

        if (shift)
        {
            var slot = _inventoryAbility.GetSlot(source.SlotIndex);
            if (slot == null || slot.Count < 2) return;

            _splitItem = slot.Item;
            _splitAmount = _inventoryAbility.SplitHalf(source.SlotIndex);
            if (_splitAmount <= 0) return;

            _isSplitDrag = true;
        }
        else
        {
            _isSplitDrag = false;
        }

        _isDragging = true;
        _dragSourceSlot = source;

        _dragIcon.sprite = source.CurrentItem.Icon;
        _dragIcon.gameObject.SetActive(true);
        _dragIcon.transform.position = Input.mousePosition;

        // 드래그 아이콘을 Canvas 루트로 올려서 다른 패널 위에 렌더링
        _dragIconOriginalParent = _dragIcon.transform.parent;
        _dragIcon.transform.SetParent(_dragIcon.canvas.transform, true);
        _dragIcon.transform.SetAsLastSibling();

        if (!_isSplitDrag)
            source.SetIconVisible(false);

        _scrollRect.enabled = false;

        if (_tooltip != null)
            _tooltip.Hide();
    }

    public void EndDrag()
    {
        if (!_isDragging) return;

        // 크로스 드래그: 인벤토리 → 창고
        if (_clickMode == EInventoryClickMode.Storage && _linkedStorage != null)
        {
            var crossTarget = _linkedStorage.HoveredSlot;
            if (crossTarget != null)
            {
                if (_isSplitDrag)
                {
                    // 마우스 포인터 위치의 창고 슬롯에 반만 배치. 슬롯이 다른 아이템이거나 꽉 찼으면 복구 후 자동 배치로 폴백
                    bool placed = _storageTransferService.AddHeldItemToStorageSlot(
                        _splitItem, crossTarget.SlotIndex, _splitAmount);
                    if (!placed)
                    {
                        _inventoryAbility.PlaceSplit(
                            _dragSourceSlot.SlotIndex, _dragSourceSlot.SlotIndex, _splitItem, _splitAmount);
                        _storageTransferService.MoveToStorage(_dragSourceSlot.SlotIndex, _splitAmount);
                    }
                }
                else
                {
                    _storageTransferService.SwapAcross(
                        _dragSourceSlot.SlotIndex, crossTarget.SlotIndex);
                }
                ClearDragState();
                return;
            }
        }

        // 일반 드래그: 인벤토리 내부
        if (_isSplitDrag)
        {
            int targetIndex = _hoveredSlot != null && _hoveredSlot != _dragSourceSlot
                ? _hoveredSlot.SlotIndex
                : _dragSourceSlot.SlotIndex;

            _inventoryAbility.PlaceSplit(_dragSourceSlot.SlotIndex, targetIndex, _splitItem, _splitAmount);
        }
        else
        {
            if (_hoveredSlot != null && _hoveredSlot != _dragSourceSlot)
                _inventoryAbility.SwapSlots(_dragSourceSlot.SlotIndex, _hoveredSlot.SlotIndex);
            else
                RefreshSlot(_dragSourceSlot.SlotIndex);
        }

        ClearDragState();
    }

    private void CancelDrag()
    {
        if (!_isDragging) return;

        if (_isSplitDrag)
            _inventoryAbility.PlaceSplit(
                _dragSourceSlot.SlotIndex, _dragSourceSlot.SlotIndex, _splitItem, _splitAmount);
        else
            RefreshSlot(_dragSourceSlot.SlotIndex);

        ClearDragState();
    }

    private void ClearDragState()
    {
        if (_dragIconOriginalParent != null)
            _dragIcon.transform.SetParent(_dragIconOriginalParent, true);
        _dragIcon.gameObject.SetActive(false);
        _scrollRect.enabled = true;
        _isDragging = false;
        _isSplitDrag = false;
        _splitItem = null;
        _splitAmount = 0;
        _dragSourceSlot = null;
    }

    // 클릭 모드

    public void SetClickMode(EInventoryClickMode mode)
    {
        _clickMode = mode;
        CancelDrag();
    }

    public void OnSlotClicked(UI_Slot clicked)
    {
        if (_isDragging) return;

        if (_clickMode == EInventoryClickMode.Normal)
        {
            if (clicked.CurrentItem is PotionDataSO)
            {
                bool used = _potionAbility != null && _potionAbility.TryUsePotion(clicked.SlotIndex);
                return;
            }
        }

        if (_clickMode == EInventoryClickMode.Trading)
        {
            if (_tradeAmountPopup != null && _tradeAmountPopup.IsOpen) return;
            HandleSellClick(clicked);
        }
    }

    public void OnSlotRightClicked(UI_Slot clicked)
    {
        if (_isDragging) return;
        if (clicked.CurrentItem == null) return;
        if (_clickMode != EInventoryClickMode.Normal) return;

        if (clicked.CurrentItem is SeedItemDataSO seedItem && RequestSeedSelection(seedItem))
            return;

        if (clicked.CurrentItem.IsGround && RequestGroundSelection(clicked.CurrentItem))
            return;

        if (clicked.CurrentItem.Type == EItemType.Fertilizer && RequestFertilizerSelection(clicked.CurrentItem))
            return;


        // Storage 모드: 인벤토리 → 창고 빠른 이동
        if (_clickMode == EInventoryClickMode.Storage)
        {
            _storageTransferService?.MoveToStorage(clicked.SlotIndex, 1);
            return;
        }

        if (!(clicked.CurrentItem is PotionDataSO)) return;

        bool used = _potionAbility != null && _potionAbility.TryUsePotion(clicked.SlotIndex);

        if (used)
        {
            Debug.Log($"물약 사용 성공 Slot: {clicked.SlotIndex}");
        }
        else
        {
            Debug.LogWarning($"물약 사용 실패");
        }
    }

    // 판매

    private bool RequestSeedSelection(SeedItemDataSO seedItem)
    {
        if (seedItem == null || SeedSelectionRequested == null)
            return false;

        foreach (Func<SeedItemDataSO, bool> handler in SeedSelectionRequested.GetInvocationList())
        {
            if (handler.Invoke(seedItem))
                return true;
        }

        return false;
    }

    private bool RequestGroundSelection(ItemDataSO groundItem)
    {
        if (groundItem == null || GroundSelectionRequested == null)
            return false;

        foreach (Func<ItemDataSO, bool> handler in GroundSelectionRequested.GetInvocationList())
        {
            if (handler.Invoke(groundItem))
                return true;
        }

        return false;
    }

    private bool RequestFertilizerSelection(ItemDataSO fertilizerItem)
    {
        if (fertilizerItem == null || FertilizerSelectionRequested == null)
            return false;

        foreach (Func<ItemDataSO, bool> handler in FertilizerSelectionRequested.GetInvocationList())
        {
            if (handler.Invoke(fertilizerItem))
                return true;
        }

        return false;
    }

    public void Init(TradeService tradeService)
    {
        _tradeService = tradeService;
    }

    public void SetLinkedStorage(UI_Storage storage, StorageTransferService service)
    {
        _linkedStorage = storage;
        _storageTransferService = service;
    }

    private void HandleStorageControllerReady(StorageController controller)
    {
        BindStorageController(controller);
    }

    private void BindStorageController(StorageController controller)
    {
        if (controller == null) return;

        if (_boundStorageController == controller)
            return;

        UnbindStorageController();

        _boundStorageController = controller;
        _boundStorageController.OnStorageOpened += HandleStorageOpened;
        _boundStorageController.OnStorageClosed += HandleStorageClosed;

        if (controller.IsOpen && controller.CurrentSession != null)
            HandleStorageOpened(controller.CurrentSession);
    }

    private void UnbindStorageController()
    {
        if (_boundStorageController == null) return;

        _boundStorageController.OnStorageOpened -= HandleStorageOpened;
        _boundStorageController.OnStorageClosed -= HandleStorageClosed;
        _boundStorageController = null;
    }

    private void HandleStorageOpened(StorageSession session)
    {
        if (session == null) return;

        // 던전 복귀 등으로 바인딩이 누락된 경우 복구
        if (_inventoryAbility == null)
        {
            TryBindExistingPlayer();
            // Open() 이벤트를 이미 놓쳤으므로 직접 패널 활성화
            if (_inventoryAbility != null && _inventoryAbility.IsOpen)
                OnToggle(true);
        }

        SetLinkedStorage(StorageUiRegistry.CurrentStorage, session.TransferService);
        SetClickMode(EInventoryClickMode.Storage);
    }

    private void HandleStorageClosed()
    {
        SetClickMode(EInventoryClickMode.Normal);
        SetLinkedStorage(null, null);
    }

    private void HandleSellClick(UI_Slot clicked)
    {
        if (_tradeService == null || _inventoryAbility == null || _tradeAmountPopup == null) return;
        if (clicked == null || clicked.CurrentItem == null) return;

        InventorySlot slot = _inventoryAbility.GetSlot(clicked.SlotIndex);
        if (slot == null || slot.IsEmpty || slot.Item == null) return;

        int maxSellAmount = slot.Count;

        if (_tradeAmountPopup.IsOpen) return;
        _tradeAmountPopup.OpenAsync(
            slot.Item,
            ETradeType.Sell,
            maxSellAmount,
            amount =>
            {
                bool success = _tradeService.Sell(clicked.SlotIndex, amount);
            }).Forget();
    }

    private void HandleStorageRegistered(UI_Storage storage)
    {
        if (_boundStorageController == null || !_boundStorageController.IsOpen) return;
        SetLinkedStorage(storage, _storageTransferService);
    }

    private void HandleStorageUnregistered()
    {
        SetLinkedStorage(null, _storageTransferService);
    }
}
