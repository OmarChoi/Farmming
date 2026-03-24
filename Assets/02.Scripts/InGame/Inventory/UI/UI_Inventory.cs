using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_Inventory : MonoBehaviour
{
    private const int COLUMNS = 4;

    [Header("참조")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Transform _slotContainer;
    [SerializeField] private UI_Slot _uiSlotPrefab;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private UI_ItemTooltip _tooltip;
    [SerializeField] private Image _dragIcon;

    [Header("슬라이드 애니메이션")]
    [SerializeField] private float _slideDuration = 0.3f;
    [SerializeField] private float _slideDistance = 300f;

    private RectTransform _panelRect;
    private Vector2 _panelOriginPos;
    private Tween _slideTween;
    private PlayerInventoryAbility _inventoryAbility;
    private readonly List<UI_Slot> _slotUIs = new();

    private TradeService _tradeService;
    private EInventoryClickMode _clickMode = EInventoryClickMode.Normal;

    // 드래그 상태
    private bool _isDragging;
    private UI_Slot _dragSourceSlot;
    private UI_Slot _hoveredSlot;

    private void Awake()
    {
        _panelRect = _panel.GetComponent<RectTransform>();
        _panelOriginPos = _panelRect.anchoredPosition;
        _panel.SetActive(false);
        _dragIcon.gameObject.SetActive(false);
        PlayerInventoryAbility.OnLocalPlayerReady += Bind;
    }

    private void OnDestroy()
    {
        PlayerInventoryAbility.OnLocalPlayerReady -= Bind;
        Unbind();
    }

    private void Update()
    {
        if (!_isDragging) return;

        _dragIcon.transform.position = Input.mousePosition;

        if (!Input.GetMouseButton(0))
            EndDrag();
    }

    private void Bind(PlayerInventoryAbility ability)
    {
        Unbind();

        _inventoryAbility = ability;
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

        int column = slot.SlotIndex % COLUMNS;
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

    public void BeginDrag(UI_Slot source)
    {
        if (_clickMode != EInventoryClickMode.Normal) return;
        if (source.CurrentItem == null) return;

        _isDragging = true;
        _dragSourceSlot = source;

        _dragIcon.sprite = source.CurrentItem.Icon;
        _dragIcon.gameObject.SetActive(true);
        _dragIcon.transform.position = Input.mousePosition;

        source.SetIconVisible(false);
        _scrollRect.enabled = false;

        if (_tooltip != null)
            _tooltip.Hide();
    }

    public void EndDrag()
    {
        if (!_isDragging) return;

        if (_hoveredSlot != null && _hoveredSlot != _dragSourceSlot)
            _inventoryAbility.SwapSlots(_dragSourceSlot.SlotIndex, _hoveredSlot.SlotIndex);

        _dragSourceSlot.SetIconVisible(true);
        RefreshSlot(_dragSourceSlot.SlotIndex);

        _dragIcon.gameObject.SetActive(false);
        _scrollRect.enabled = true;
        _isDragging = false;
        _dragSourceSlot = null;
    }

    private void CancelDrag()
    {
        if (!_isDragging) return;

        _dragSourceSlot.SetIconVisible(true);
        RefreshSlot(_dragSourceSlot.SlotIndex);

        _dragIcon.gameObject.SetActive(false);
        _scrollRect.enabled = true;
        _isDragging = false;
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

        if (_clickMode == EInventoryClickMode.Trading)
            HandleSellClick(clicked);
    }

    // 판매

    public void Init(TradeService tradeService)
    {
        _tradeService = tradeService;
    }

    private void HandleSellClick(UI_Slot clicked)
    {
        if (_tradeService == null) return;

        bool success = _tradeService.Sell(clicked.SlotIndex, 1);

#if UNITY_EDITOR
        if (success)
            Debug.Log($"판매 성공 - Slot: {clicked.SlotIndex}, Amount: 1");
        else
            Debug.LogWarning($"판매 실패 - Slot: {clicked.SlotIndex}");
#endif
    }
}