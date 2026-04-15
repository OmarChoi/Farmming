using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class UI_VillageLevel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const string LevelFormat = "LV.{0:0}";
    private const string MaxLevelLabel = "MAX";

    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Image _fillImage;

    private void OnEnable()
    {
        if (VillageLevelManager.Instance == null) return;
        SubScribeEvents();
        UpdateGauge();
    }

    private void Start()
    {
        SubScribeEvents();
        UpdateGauge();
    }

    private void OnDisable()
    {
        if (VillageLevelManager.Instance != null)
        {
            VillageLevelManager.Instance.OnVillageStateChanged -= UpdateGauge;
        }
    }

    private void SubScribeEvents()
    {
        if (VillageLevelManager.Instance == null) return;
        VillageLevelManager.Instance.OnVillageStateChanged -= UpdateGauge;
        VillageLevelManager.Instance.OnVillageStateChanged += UpdateGauge;
    }

    private void UpdateGauge()
    {
        VillageLevelManager manager = VillageLevelManager.Instance;
        if (manager == null) return;

        if (manager.IsMaxLevel)
        {
            _fillImage.fillAmount = 1f;
            _levelText.SetText(MaxLevelLabel);
            return;
        }

        _fillImage.fillAmount = (float)manager.CurrentVitality / manager.CurrentVitalityThreshold;
        _levelText.SetText(LevelFormat, manager.CurrentLevel);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UIController.Instance.OpenAsync(new UILifecycleActions<UI_VillageState>
        {
            OnOpen = ui => ui.transform.position = eventData.position,
        }).Forget();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIController.Instance.CloseAsync<UI_VillageState>().Forget();
    }
}
