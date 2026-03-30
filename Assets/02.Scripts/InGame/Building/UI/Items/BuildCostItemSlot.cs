using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildCostItemSlot : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _amountLabel;

    public void Refresh(BuildingCostEntry cost)
    {
        if (cost.Item == null)
        {
            _icon.enabled = false;
            _amountLabel.text = string.Empty;
            return;
        }

        _icon.enabled = true;
        _icon.sprite = cost.Item.Icon;
        _amountLabel.text = $"{cost.Amount}개";
    }
}
