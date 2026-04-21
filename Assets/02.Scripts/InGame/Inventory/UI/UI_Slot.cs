using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UI_Slot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _countText;

    private ISlotContainer _container;
    private int _slotIndex;
    private ItemDataSO _currentItem;

    public int SlotIndex => _slotIndex;
    public ItemDataSO CurrentItem => _currentItem;
    public RectTransform RectTransform => (RectTransform)transform;

    public void Init(ISlotContainer container, int index)
    {
        _container = container;
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

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        if (_currentItem == null) return;
        if (_currentItem is PotionDataSO) return;

        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        _container.BeginDrag(this, shift);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        _container.EndDrag();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _container.OnSlotRightClicked(this);
        }
        else
        { 
            _container.OnSlotClicked(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _container.OnSlotHoverEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _container.OnSlotHoverExit();
    }
}
