using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_VillageLevel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Image _fillImage;

    private void Start()
    {
        VillageLevelManager.Instance.OnVillageStateChanged += UpdateGauge;
        UpdateGauge();
    }

    private void OnDestroy()
    {
        if (VillageLevelManager.Instance != null)
        {
            VillageLevelManager.Instance.OnVillageStateChanged -= UpdateGauge;
        }
    }
    
    private void UpdateGauge()
    {
        int level = VillageLevelManager.Instance.CurrentLevel;
        int currentGauge = VillageLevelManager.Instance.CurrentGauge;
        int totalGauge = VillageLevelManager.Instance.CurrentThreshold;
        _fillImage.fillAmount = totalGauge > 0 ? (float)currentGauge / totalGauge : 1f;
        _levelText.text = $"LV.{level.ToString().PadLeft(2, '0')}";
    }
}
