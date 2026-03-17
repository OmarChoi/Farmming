using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_ShopItemSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _itemNameText;
    [SerializeField] private TextMeshProUGUI _itemCostText;
    [SerializeField] private TextMeshProUGUI _itemExplanationText;

    private UI_Shop _uiShop;
    private int _slotIndex;
    private ItemDataSO _itemData;

    public int SlotIndex => _slotIndex;
    public ItemDataSO ItemData => _itemData;

    public void Init(UI_Shop uiShop, int index)
    {
        _uiShop = uiShop;
        _slotIndex = index;
    }

    public void Refresh(ItemDataSO item)
    {
        _itemData = item;

        if (item == null)
        {
            _icon.enabled = false;
            _itemNameText.text = "";
            _itemCostText.text = "";
            _itemExplanationText.text = "";
            return;
        }

        _icon.enabled = true;
        _icon.sprite = item.Icon;
        _itemNameText.text = $"{item.name}";
        _itemCostText.text = $"Cost: {item.Cost}";
        _itemExplanationText.text = $"{item.DisplayExplanation}";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _uiShop.OnShopSlotClicked(this);
    }
}
