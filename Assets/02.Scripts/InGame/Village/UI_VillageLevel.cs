using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD에 항상 떠 있는 마을 레벨 배지와 활력 게이지.
/// VillageLevelManager의 상태 변경 이벤트만 구독해 숫자/fillAmount를 갱신하며,
/// 세부 수치(활력 임계치, 건물 수 등)는 Tab 토글로 여는 UI_InfoPannel의
/// UI_VillageInfo 섹션에서 확인한다.
/// </summary>
public class UI_VillageLevel : MonoBehaviour
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
}
