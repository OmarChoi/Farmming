using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UI_Slot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _countText;

    private UI_Inventory _uiInventory;
    private int _slotIndex;

    public int SlotIndex => _slotIndex;

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
            return;
        }

        _icon.enabled = true;
        _icon.sprite = slot.Item.Icon;
        _countText.text = slot.Count > 1 ? slot.Count.ToString() : "";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _uiInventory.OnSlotClicked(this);
    }
}