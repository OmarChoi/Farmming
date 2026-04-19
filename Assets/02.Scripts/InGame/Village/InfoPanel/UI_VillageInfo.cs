using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_VillageInfo : UI_InfoSectionBase
{
    private const string LevelFormat = "Level {0:0}";
    private const string GaugeFormat = "{0:0} / {1:0}";
    private const string MaxLevelLabel = "MAX LEVEL";
    private const string EmptyLabel = "-";

    [Header("Village Infos")]
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _vitalityText;
    [SerializeField] private TextMeshProUGUI _buildingCountText;

    [Header("Gauges")]
    [SerializeField] private Image _vitalityGaugeImage;
    [SerializeField] private Image _buildingCountGaugeImage;

    [Header("Animation")]
    [SerializeField] private float _gaugeDuration = 0.4f;

    public override void Subscribe(PlayerController playerController)
    {
        VillageLevelManager manager = VillageLevelManager.Instance;
        if (manager == null) return;

        manager.OnVillageStateChanged -= HandleStateChanged;
        manager.OnVillageStateChanged += HandleStateChanged;
    }
    
    public override void Unsubscribe()
    {
        VillageLevelManager manager = VillageLevelManager.Instance;
        if (manager != null) manager.OnVillageStateChanged -= HandleStateChanged;

        KillGaugeTweens();
    }
    
    public override void Refresh()
    {
        VillageLevelManager manager = VillageLevelManager.Instance;

        if (manager == null)
        {
            ApplyEmpty();
            return;
        }

        _levelText?.SetText(LevelFormat, manager.CurrentLevel);

        if (manager.IsMaxLevel)
        {
            _vitalityText?.SetText(MaxLevelLabel);
            _buildingCountText?.SetText(MaxLevelLabel);
            AnimateGauge(_vitalityGaugeImage, 1f);
            AnimateGauge(_buildingCountGaugeImage, 1f);
            return;
        }

        LevelUpRequirement needs = manager.LevelUpRequirement;
        int currentBuilding = BuildingManager.Instance != null ? BuildingManager.Instance.BuildingCount : 0;

        _vitalityText?.SetText(GaugeFormat, manager.CurrentVitality, needs.VitalityThreshold);
        _buildingCountText?.SetText(GaugeFormat, currentBuilding, needs.BuildingCountThreshold);

        AnimateGauge(_vitalityGaugeImage, needs.GetVitalityRatio(manager.CurrentVitality));
        AnimateGauge(_buildingCountGaugeImage, needs.GetBuildingRatio(currentBuilding));
    }

    private void HandleStateChanged()
    {
        Refresh();
    }

    private void AnimateGauge(Image gauge, float target)
    {
        if (gauge == null) return;
        gauge.DOKill();
        gauge.DOFillAmount(target, _gaugeDuration).SetEase(Ease.OutCubic).SetUpdate(true);
    }

    private void KillGaugeTweens()
    {
        _vitalityGaugeImage?.DOKill();
        _buildingCountGaugeImage?.DOKill();
    }

    private void ApplyEmpty()
    {
        _levelText?.SetText(EmptyLabel);
        _vitalityText?.SetText(EmptyLabel);
        _buildingCountText?.SetText(EmptyLabel);

        KillGaugeTweens();
        if (_vitalityGaugeImage != null) _vitalityGaugeImage.fillAmount = 0f;
        if (_buildingCountGaugeImage != null) _buildingCountGaugeImage.fillAmount = 0f;
    }
}
