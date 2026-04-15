using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_ShopItemSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
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
            _iconImage.enabled = false;
            _itemNameText.text = ItemDisplayFormatter.GetName(null);
            _itemCostText.text = ItemDisplayFormatter.GetBuyCostText(null);
            _itemExplanationText.text = ItemDisplayFormatter.GetExplanation(null);
            return;
        }

        _iconImage.enabled = true;
        _iconImage.sprite = ItemDisplayFormatter.GetIcon(item);
        _itemNameText.text = ItemDisplayFormatter.GetName(item);
        _itemCostText.text = ItemDisplayFormatter.GetBuyCostText(item);
        _itemExplanationText.text = ItemDisplayFormatter.GetExplanation(item);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _uiShop.OnShopSlotClicked(this);
    }
}
