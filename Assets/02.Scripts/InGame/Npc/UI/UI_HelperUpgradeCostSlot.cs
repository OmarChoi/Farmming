using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_HelperUpgradeCostSlot : MonoBehaviour
{
    [Header("아이템 이미지")]
    [SerializeField] private Image _itemIcon;

    [Header("수량 텍스트")]
    [SerializeField] private TextMeshProUGUI _amountText;

    public void BindCostSlot(Sprite icon, int ownedCount, int requiredCount, bool enough)
    {
        if (_itemIcon != null)
        {
            _itemIcon.sprite = icon;
            _itemIcon.enabled = icon != null;
        }

        if (_amountText != null)
        {
            _amountText.text = $"{ownedCount} / {requiredCount}";
            _amountText.color = enough ? Color.white : Color.red;
        }
    }
}
