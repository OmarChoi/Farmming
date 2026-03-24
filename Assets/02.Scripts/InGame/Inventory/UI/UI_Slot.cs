using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UI_Slot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _countText;

    private UI_Inventory _uiInventory;
    private int _slotIndex;
    private ItemDataSO _currentItem;

    private bool _isPointerDown;
    private float _pointerDownTime;

    public int SlotIndex => _slotIndex;
    public ItemDataSO CurrentItem => _currentItem;
    public RectTransform RectTransform => (RectTransform)transform;

    public void Init(UI_Inventory uiInventory, int index)
    {
        _uiInventory = uiInventory;
        _slotIndex = index;
    }

    public void Refresh(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty)
        {
            _icon.enabled = false;
            _countText.text = "";
            _currentItem = null;
            return;
        }

        _icon.enabled = true;
        _icon.sprite = slot.Item.Icon;
        _countText.text = slot.Count > 1 ? slot.Count.ToString() : "";
        _currentItem = slot.Item;
    }

    public void SetIconVisible(bool visible)
    {
        _icon.enabled = visible && _currentItem != null;
        _countText.text = visible && _currentItem != null ? _countText.text : "";
    }

    private void Update()
    {
        if (!_isPointerDown) return;

        if (Time.unscaledTime - _pointerDownTime >= _uiInventory.HoldDuration)
        {
            _isPointerDown = false;
            _uiInventory.BeginDrag(this);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_currentItem == null) return;
        _isPointerDown = true;
        _pointerDownTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPointerDown = false;
        _uiInventory.EndDrag();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _uiInventory.OnSlotClicked(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _uiInventory.OnSlotHoverEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _uiInventory.OnSlotHoverExit();
    }
}