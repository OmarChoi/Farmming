using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// 창고 아이템 패널. 슬롯 관리 + 드래그앤드롭 로직 담당.
/// UI_Storage가 세션을 주입하면 이 패널이 실제 동작을 수행.
public class UI_StorageItemPanel : MonoBehaviour, ISlotContainer
{
    private const int Columns = 4;

    [Header("슬롯")]
    [SerializeField] private Transform _slotContainer;
    [SerializeField] private UI_Slot _slotPrefab;
    [SerializeField] private ScrollRect _scrollRect;

    [Header("드래그 / 툴팁")]
    [SerializeField] private UI_ItemTooltip _tooltip;
    [SerializeField] private Image _dragIcon;

    private StorageDomain _storage;
    private StorageTransferService _transferService;
    private readonly List<UI_Slot> _slotUIs = new();

    private bool _isDragging;
    private bool _isSplitDrag;
    private UI_Slot _dragSourceSlot;
    private UI_Slot _hoveredSlot;
    private Transform _dragIconOriginalParent;
    private bool _suppressRefresh;

    private ItemDataSO _dragItem;
    private int _dragCount;

    private UI_Inventory _linkedInventory;

    public bool IsDragging => _isDragging;
    public UI_Slot HoveredSlot => _hoveredSlot;

    private void Awake()
    {
        if (_dragIcon != null)
            _dragIcon.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_isDragging) return;

        if (_dragIcon != null)
            _dragIcon.transform.position = Input.mousePosition;

        if (!Input.GetMouseButton(0))
            EndDrag();
    }

    private void OnDestroy()
    {
        Unbind();
    }

    public void Init(StorageDomain storage, StorageTransferService transferService)
    {
        Unbind();

        _storage = storage;
        _transferService = transferService;

        if (_storage != null)
        {
            _storage.OnSlotChanged += RefreshSlot;
            _storage.OnSlotCountChanged += OnSyncReceived;
        }

        BuildSlots();
        RefreshAll();
    }

    public void Unbind()
    {
        if (_storage != null)
        {
            _storage.OnSlotChanged -= RefreshSlot;
            _storage.OnSlotCountChanged -= OnSyncReceived;
        }
        _storage = null;
        _transferService = null;
    }

    public void SetLinkedInventory(UI_Inventory inventory)
    {
        _linkedInventory = inventory;
    }

    public void HideTooltip()
    {
        if (_tooltip != null)
            _tooltip.Hide();
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

        if (_linkedInventory != null)
        {
            var crossTarget = _linkedInventory.HoveredSlot;
            if (crossTarget != null)
            {
                if (_isSplitDrag)
                {
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

        {
            int targetIndex = _hoveredSlot != null && _hoveredSlot != _dragSourceSlot
                ? _hoveredSlot.SlotIndex
                : _dragSourceSlot.SlotIndex;

            if (_isSplitDrag)
                _transferService.PlaceSplitInStorage(_dragSourceSlot.SlotIndex, targetIndex, _dragItem, _dragCount);
            else
                _transferService.PutDownInStorage(targetIndex, _dragItem, _dragCount);
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

    public void OnSlotClicked(UI_Slot clicked) { }

    public void OnSlotRightClicked(UI_Slot clicked)
    {
        if (_isDragging) return;
        if (clicked.CurrentItem == null) return;

        _transferService?.MoveToInventory(clicked.SlotIndex, 1);
    }

    private void OnSyncReceived()
    {
        if (_suppressRefresh) return;
        RefreshAll();
    }
}