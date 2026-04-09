using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_HelperExperience : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private float _tweenDuration = 0.5f;
    [SerializeField] private GameObject _upgradeReadyIndicator;
    [SerializeField] private TMP_Text _maxText;
    [SerializeField] private Color _legendaryMaxColor = new Color(0f, 0.85f, 1f, 1f);
    [SerializeField] private Color _groundHelperColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color _maxTextColor = Color.white;
    [SerializeField] private float _maxTextFontSize = 12f;

    private PlayerHelperInventoryAbility _ability;
    private HelperController _helper;
    private HelperExperience _experience;
    private Tweener _fillTween;
    private Tweener _colorTween;
    private Color _defaultFillColor;

    private void Awake()
    {
        _defaultFillColor = _fillImage.color;
        _fillImage.fillAmount = 0f;

        EnsureMaxText();
        SetMaxTextVisible(false);
        SetFillVisible(true);
        SetUpgradeReadyIndicatorVisible(false);

        PlayerHelperInventoryAbility.OnLocalPlayerReady += Bind;
    }

    private void OnDestroy()
    {
        _fillTween?.Kill();
        _colorTween?.Kill();
        PlayerHelperInventoryAbility.OnLocalPlayerReady -= Bind;
        Unbind();
    }

    private void Bind(PlayerHelperInventoryAbility ability)
    {
        Unbind();

        _ability = ability;
        _ability.OnSummonChanged += OnSummonChanged;

        if (_ability.ActiveMainHelper != null)
            BindHelper(_ability.ActiveMainHelper);
    }

    private void Unbind()
    {
        if (_ability == null)
            return;

        _ability.OnSummonChanged -= OnSummonChanged;
        _ability = null;
        UnbindExperience();
    }

    private void OnSummonChanged(int summonedIndex)
    {
        UnbindExperience();

        if (_ability?.ActiveMainHelper != null)
        {
            BindHelper(_ability.ActiveMainHelper);
            return;
        }

        SetFillColor(_defaultFillColor, false);
        AnimateFill(0f);
        SetMaxTextVisible(false);
        SetUpgradeReadyIndicatorVisible(false);
    }

    private void BindHelper(HelperController helper)
    {
        _helper = helper;
        _experience = helper.Experience;
        _helper.OnGradeChanged += RefreshDisplayState;
        _experience.OnExpChanged += UpdateFill;
        _experience.OnReadyToUpgrade += OnReadyToUpgrade;

        _fillTween?.Kill();
        RefreshDisplayState();
    }

    private void UnbindExperience()
    {
        if (_experience == null)
            return;

        _experience.OnExpChanged -= UpdateFill;
        _experience.OnReadyToUpgrade -= OnReadyToUpgrade;

        if (_helper != null)
            _helper.OnGradeChanged -= RefreshDisplayState;

        _helper = null;
        _experience = null;
    }

    private void UpdateFill(int current, int max)
    {
        if (TryApplyFixedState())
            return;

        if (max <= 0)
        {
            AnimateFill(0f);
            return;
        }

        SetFillColor(_defaultFillColor, false);
        SetMaxTextVisible(false);
        AnimateFill((float)current / max);
    }

    private void RefreshDisplayState()
    {
        if (TryApplyFixedState())
            return;

        SetFillVisible(true);
        SetFillColor(_defaultFillColor, false);
        SetMaxTextVisible(false);
        SetUpgradeReadyIndicatorVisible(_experience != null && _experience.IsReadyToUpgrade);

        float ratio = _experience != null && _experience.MaxExp > 0
            ? (float)_experience.CurrentExp / _experience.MaxExp
            : 0f;

        _fillImage.fillAmount = ratio;
    }

    private bool TryApplyFixedState()
    {
        if (_helper == null || _helper.Data == null)
            return false;

        if (_helper.Data.UsesGroundFixedExpBar)
        {
            ApplyFixedFill(_groundHelperColor, false);
            return true;
        }

        if (_helper.Grade.CurrentGrade == EHelperGrade.Legendary
            && _helper.Data.UsesLegendaryMaxExpBar)
        {
            ApplyFixedFill(_legendaryMaxColor, true);
            return true;
        }

        return false;
    }

    private void ApplyFixedFill(Color color, bool showMaxText)
    {
        SetFillVisible(true);
        SetFillColor(color, false);
        _fillImage.fillAmount = 1f;
        SetMaxTextVisible(showMaxText);
        SetUpgradeReadyIndicatorVisible(false);
    }

    private void OnReadyToUpgrade()
    {
        SetUpgradeReadyIndicatorVisible(true);
    }

    private void AnimateFill(float targetRatio)
    {
        _fillTween?.Kill();
        _fillTween = _fillImage.DOFillAmount(targetRatio, _tweenDuration).SetEase(Ease.OutCubic);
    }

    private void SetFillColor(Color color, bool animate)
    {
        _colorTween?.Kill();

        if (animate)
            _colorTween = _fillImage.DOColor(color, _tweenDuration).SetEase(Ease.OutCubic);
        else
            _fillImage.color = color;
    }

    private void SetFillVisible(bool visible)
    {
        if (_fillImage != null)
            _fillImage.gameObject.SetActive(visible);
    }

    private void SetUpgradeReadyIndicatorVisible(bool visible)
    {
        if (_upgradeReadyIndicator == null || _fillImage == null)
            return;

        if (_upgradeReadyIndicator == _fillImage.gameObject)
        {
            SetFillVisible(true);
            return;
        }

        _upgradeReadyIndicator.SetActive(visible);
    }

    private void EnsureMaxText()
    {
        if (_maxText != null)
            return;

        Transform textParent = _fillImage != null ? _fillImage.transform : transform;
        var textObject = new GameObject("MaxText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(textParent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _maxText = textObject.GetComponent<TextMeshProUGUI>();
        _maxText.text = "MAX";
        _maxText.alignment = TextAlignmentOptions.Center;
        _maxText.fontSize = _maxTextFontSize;
        _maxText.color = _maxTextColor;
        _maxText.raycastTarget = false;
    }

    private void SetMaxTextVisible(bool visible)
    {
        if (_maxText == null)
            return;

        _maxText.text = "MAX";
        _maxText.gameObject.SetActive(visible);
    }
}
