using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_Storage : MonoBehaviour, ISlotContainer
{
    private const int Columns = 4;

    [Header("참조")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Transform _slotContainer;
    [SerializeField] private UI_Slot _slotPrefab;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private UI_ItemTooltip _tooltip;
    [SerializeField] private Image _dragIcon;

    [Header("닫기 버튼")]
    [SerializeField] private Button _exitButton;

    [Header("슬라이드 애니메이션")]
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private float _slideDistance = 300f;

    private RectTransform _panelRect;
    private Vector2 _panelOriginPos;
    private Tween _slideTween;

    private StorageDomain _storage;
    private StorageTransferService _transferService;
    private StorageController _boundController;
    private readonly List<UI_Slot> _slotUIs = new();

    // 드래그 상태
    private bool _isDragging;
    private bool _isSplitDrag;
    private UI_Slot _dragSourceSlot;
    private UI_Slot _hoveredSlot;
    private Transform _dragIconOriginalParent;
    private bool _suppressRefresh;

    // 드래그 중인 아이템 (일반 / 분할 동일)
    private ItemDataSO _dragItem;
    private int _dragCount;

    // 크로스 드래그용
    private UI_Inventory _linkedInventory;

    public bool IsDragging => _isDragging;
    public UI_Slot HoveredSlot => _hoveredSlot;

    public void SetLinkedInventory(UI_Inventory inventory)
    {
        _linkedInventory = inventory;
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
        if (_dragIcon != null)
            _dragIcon.gameObject.SetActive(false);
        if (_exitButton != null)
            _exitButton.onClick.AddListener(HandleCloseRequested);
    }

    private void Update()
    {
        if (!_isDragging) return;

        if (_dragIcon != null)
            _dragIcon.transform.position = Input.mousePosition;

        if (!Input.GetMouseButton(0))
            EndDrag();
    }

    public void Init(StorageDomain storage, StorageTransferService transferService)
    {
        if (_storage != null)
        {
            _storage.OnSlotChanged -= RefreshSlot;
            _storage.OnSlotCountChanged -= OnSyncReceived;
        }

        _storage = storage;
        _transferService = transferService;

        if (_storage != null)
        {
            _storage.OnSlotChanged += RefreshSlot;
            _storage.OnSlotCountChanged += OnSyncReceived;
        }

        BuildSlots();
    }

    public void Open()
    {
        _slideTween?.Kill();
        _panel.SetActive(true);
        RefreshAll();

        _panelRect.anchoredPosition = _panelOriginPos + Vector2.left * _slideDistance;
        _slideTween = _panelRect.DOAnchorPos(_panelOriginPos, _slideDuration)
            .SetEase(Ease.OutBack)
            .SetLink(_panel);
    }

    public void Close()
    {
        _slideTween?.Kill();
        CancelDrag();
        if (_tooltip != null)
        {
            _tooltip.Hide();
        }

        Vector2 target = _panelOriginPos + Vector2.left * _slideDistance;
        _slideTween = _panelRect.DOAnchorPos(target, _slideDuration)
            .SetEase(Ease.InBack)
            .SetLink(_panel)
            .OnComplete(() => _panel.SetActive(false));
    }

    private void BuildSlots()
    {
        foreach (var slot in _slotUIs)
            Destroy(slot.gameObject);
        _slotUIs.Clear();

        if (_storage == null) return;

        for (int i = 0; i < _storage.SlotCount; i++)
        {
            var slotUI = Instantiate(_slotPrefab, _slotContainer);
            slotUI.Init(this, i);
            _slotUIs.Add(slotUI);
        }
    }

    private void RefreshAll()
    {
        for (int i = 0; i < _slotUIs.Count; i++)
            RefreshSlot(i);
    }

    private void RefreshSlot(int index)
    {
        if (_suppressRefresh) return;
        if (index < 0 || index >= _slotUIs.Count) return;
        _slotUIs[index].Refresh(_storage.GetSlot(index));
    }

    // ISlotContainer 구현

    public void BeginDrag(UI_Slot source, bool shift)
    {
        if (source.CurrentItem == null) return;

        if (shift)
        {
            int half = _transferService.SplitHalfInStorage(source.SlotIndex);
            if (half <= 0) return;

            _dragItem = source.CurrentItem;
            _dragCount = half;
        }
        else
        {
            // 도메인에서 아이템을 꺼내고 동기화
            _transferService.PickUpFromStorage(source.SlotIndex, out _dragItem, out _dragCount);
            if (_dragItem == null || _dragCount <= 0) return;
        }

        _isDragging = true;
        _isSplitDrag = shift;
        _dragSourceSlot = source;

        if (_dragIcon != null)
        {
            _dragIcon.sprite = _dragItem.Icon;
            _dragIcon.gameObject.SetActive(true);
            _dragIcon.transform.position = Input.mousePosition;

            _dragIconOriginalParent = _dragIcon.transform.parent;
            _dragIcon.transform.SetParent(_dragIcon.canvas.transform, true);
            _dragIcon.transform.SetAsLastSibling();
        }

        if (_scrollRect != null)
            _scrollRect.enabled = false;
    }

    public void EndDrag()
    {
        if (!_isDragging) return;

        // 크로스 드래그: 창고 → 인벤토리
        if (_linkedInventory != null)
        {
            var crossTarget = _linkedInventory.HoveredSlot;
            if (crossTarget != null)
            {
                if (_isSplitDrag)
                {
                    // 분할 드래그: 반만 놓은 인벤토리 슬롯으로 이동 (소스에 나머지 반 유지)
                    _transferService.AddHeldItemToInventory(_dragItem, _dragCount, crossTarget.SlotIndex);
                }
                else if (!_transferService.RequiresRestoreBeforeCrossSwap)
                {
                    _transferService.SwapAcross(
                        crossTarget.SlotIndex, _dragSourceSlot.SlotIndex, preferInventory: true);
                }
                else
                {
                    _suppressRefresh = true;
                    try
                    {
                        _transferService.PutDownInStorage(_dragSourceSlot.SlotIndex, _dragItem, _dragCount);
                        _transferService.SwapAcross(
                            crossTarget.SlotIndex, _dragSourceSlot.SlotIndex, preferInventory: true);
                    }
                    finally
                    {
                        _suppressRefresh = false;
                        RefreshAll();
                    }
                }
                ClearDragState();
                return;
            }
        }

        // 창고 내부 드래그
        {
            int targetIndex = _hoveredSlot != null && _hoveredSlot != _dragSourceSlot
                ? _hoveredSlot.SlotIndex
                : _dragSourceSlot.SlotIndex;

            if (_isSplitDrag)
            {
                _transferService.PlaceSplitInStorage(_dragSourceSlot.SlotIndex, targetIndex, _dragItem, _dragCount);
            }
            else
            {
                _transferService.PutDownInStorage(targetIndex, _dragItem, _dragCount);
            }
        }

        ClearDragState();
    }

    public void CancelDrag()
    {
        if (!_isDragging) return;

        if (_isSplitDrag)
            _transferService.PlaceSplitInStorage(_dragSourceSlot.SlotIndex, _dragSourceSlot.SlotIndex, _dragItem, _dragCount);
        else
            _transferService.PutDownInStorage(_dragSourceSlot.SlotIndex, _dragItem, _dragCount);
        ClearDragState();
    }

    private void ClearDragState()
    {
        if (_dragIcon != null)
        {
            if (_dragIconOriginalParent != null)
                _dragIcon.transform.SetParent(_dragIconOriginalParent, true);
            _dragIcon.gameObject.SetActive(false);
        }
        if (_scrollRect != null)
            _scrollRect.enabled = true;

        _isDragging = false;
        _isSplitDrag = false;
        _dragItem = null;
        _dragCount = 0;
        _dragSourceSlot = null;
    }

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

    public void OnSlotClicked(UI_Slot clicked)
    {
        // 좌클릭 — 현재는 별도 동작 없음
    }

    /// 우클릭: 창고 → 인벤토리로 빠른 이동
    public void OnSlotRightClicked(UI_Slot clicked)
    {
        if (_isDragging) return;
        if (clicked.CurrentItem == null) return;

        _transferService?.MoveToInventory(clicked.SlotIndex, 1);
    }

    /// ReplaceAll(동기화 수신) 시 전체 UI 갱신
    private void OnSyncReceived()
    {
        if (_suppressRefresh) return;
        RefreshAll();
    }

    private void OnDestroy()
    {
        if (_exitButton != null)
            _exitButton.onClick.RemoveListener(HandleCloseRequested);

        StorageUiRegistry.UnregisterStorage(this);
        StorageController.OnReady -= HandleControllerReady;
        StorageUiRegistry.OnInventoryRegistered -= HandleInventoryRegistered;
        StorageUiRegistry.OnInventoryUnregistered -= HandleInventoryUnregistered;
        UnbindController();

        if (_storage != null)
        {
            _storage.OnSlotChanged -= RefreshSlot;
            _storage.OnSlotCountChanged -= OnSyncReceived;
        }
    }

    private void HandleControllerReady(StorageController controller)
    {
        BindController(controller);
    }

    private void BindController(StorageController controller)
    {
        if (controller == null) return;

        if (_boundController == controller)
            return;

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

        Init(session.Storage, session.TransferService);
        SetLinkedInventory(StorageUiRegistry.CurrentInventory);
        Open();
    }

    private void HandleStorageClosed()
    {
        SetLinkedInventory(null);
        Close();
    }

    private static void HandleCloseRequested()
    {
        StorageController.Instance?.CloseStorage();
    }

    private void HandleInventoryRegistered(UI_Inventory inventory)
    {
        if (_boundController == null || !_boundController.IsOpen) return;
        SetLinkedInventory(inventory);
    }

    private void HandleInventoryUnregistered()
    {
        SetLinkedInventory(null);
    }
}
