using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class UI_VillageLevel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        // todo. Tooltip 활성화
        UIController.Instance.OpenAsync<UI_VillageState>
        (
            ui => ui.transform.position = eventData.position
        ).Forget();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // todo. Tooltip 비활성화
        UIController.Instance.CloseAsync<UI_VillageState>().Forget();
    }
}