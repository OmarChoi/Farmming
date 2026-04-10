using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_UnlockedItem : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _description;
    
    public void SetInfo(BuildingDataSO building)
    {
        _icon.sprite = building.Icon;
        _description.text = $"{building.DisplayName} 해금됨";
    }
}
