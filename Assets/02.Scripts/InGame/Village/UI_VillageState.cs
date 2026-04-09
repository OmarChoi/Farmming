using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_VillageState : UIBase
{
    private const string LevelFormat = "Level {0:0}";
    private const string GaugeFormat = "{0:0} / {1:0}";
    private const string MaxLevelLabel = "MAX LEVEL";

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _currentLevelText;
    [SerializeField] private Image _vitalityGaugeImage;
    [SerializeField] private TextMeshProUGUI _vitalityGaugeText;
    
    [SerializeField] private Image _buildingCountGaugeImage;
    [SerializeField] private TextMeshProUGUI _buildingCountText;
    
    [Header("Animation")]
    [SerializeField] private float _animationDuration = 0.25f;
    [SerializeField] private float _gaugeDuration = 0.4f;
    
    protected override async UniTask OnOpenAnimation()
    {
        transform.localScale = Vector3.zero;
        await transform.DOScale(Vector3.one, _animationDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .AsyncWaitForCompletion();
    }

    protected override async UniTask OnCloseAnimation()
    {
        await transform.DOScale(Vector3.zero, _animationDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .AsyncWaitForCompletion();
    }

    protected override void OnOpen()
    {
        VillageLevelManager.Instance.OnVillageStateChanged += UpdateState;
        UpdateState();
    }

    protected override void OnClose()
    {
        if (VillageLevelManager.Instance != null)
        {
            VillageLevelManager.Instance.OnVillageStateChanged -= UpdateState;
        }
    }

    private void UpdateState()
    {
        VillageLevelManager villageManager = VillageLevelManager.Instance;
        int currentLevel = villageManager.CurrentLevel;

        _currentLevelText.SetText(LevelFormat, currentLevel);

        if (villageManager.IsMaxLevel)
        {
            _vitalityGaugeText.SetText(MaxLevelLabel);
            _vitalityGaugeImage.DOFillAmount(1f, _gaugeDuration).SetEase(Ease.OutCubic).SetUpdate(true);

            _buildingCountText.SetText(MaxLevelLabel);
            _buildingCountGaugeImage.DOFillAmount(1f, _gaugeDuration).SetEase(Ease.OutCubic).SetUpdate(true);
            return;
        }

        LevelUpRequirement needs = villageManager.LevelUpRequirement;
        int currentVitality = villageManager.CurrentVitality;
        int currentBuilding = BuildingManager.Instance.BuildingCount;

        _vitalityGaugeText.SetText(GaugeFormat, currentVitality, needs.VitalityThreshold);
        _vitalityGaugeImage.DOFillAmount(needs.GetVitalityRatio(currentVitality), _gaugeDuration).SetEase(Ease.OutCubic).SetUpdate(true);

        _buildingCountText.SetText(GaugeFormat, currentBuilding, needs.BuildingCountThreshold);
        _buildingCountGaugeImage.DOFillAmount(needs.GetBuildingRatio(currentBuilding), _gaugeDuration).SetEase(Ease.OutCubic).SetUpdate(true);
    }
}