using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_HelperEnergy : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private Color _normalColor = Color.cyan;
    [SerializeField] private Color _exhaustedColor = Color.gray;
    [SerializeField] private float _exhaustedTweenDuration = 0.3f;

    private PlayerHelperInventoryAbility _ability;
    private HelperEnergy _energy;
    private Tweener _colorTween;

    private void Awake()
    {
        _fillImage.fillAmount = 0f;
        _fillImage.color = _normalColor;

        PlayerHelperInventoryAbility.OnLocalPlayerReady += Bind;
    }

    private void OnDestroy()
    {
        _colorTween?.Kill();
        PlayerHelperInventoryAbility.OnLocalPlayerReady -= Bind;
        Unbind();
    }

    private void Update()
    {
        if (_energy == null)
        {
            return;
        }

        float ratio = _energy.Max > 0f ? _energy.Current / _energy.Max : 0f;
        _fillImage.fillAmount = ratio;
    }

    private void Bind(PlayerHelperInventoryAbility ability)
    {
        Unbind();

        _ability = ability;
        _ability.OnSummonChanged += OnSummonChanged;

        if (_ability.ActiveMainHelper != null)
        {
            BindEnergy(_ability.ActiveMainHelper.Energy);
        }
    }

    private void Unbind()
    {
        if (_ability == null) return;

        _ability.OnSummonChanged -= OnSummonChanged;
        _ability = null;
        UnbindEnergy();
    }

    private void OnSummonChanged(int summonedIndex)
    {
        UnbindEnergy();

        if (_ability?.ActiveMainHelper != null)
        {
            BindEnergy(_ability.ActiveMainHelper.Energy);
        }
        else
        {
            _fillImage.fillAmount = 0f;
            SetColor(_normalColor);
        }
    }

    private void BindEnergy(HelperEnergy energy)
    {
        _energy = energy;
        _energy.OnExhausted += OnEnergyExhausted;
        _energy.OnRecovered += OnEnergyRecovered;

        _fillImage.fillAmount = _energy.Max > 0f ? _energy.Current / _energy.Max : 0f;
        SetColor(_energy.IsExhausted ? _exhaustedColor : _normalColor);
    }

    private void UnbindEnergy()
    {
        if (_energy == null) return;

        _energy.OnExhausted -= OnEnergyExhausted;
        _energy.OnRecovered -= OnEnergyRecovered;
        _energy = null;
    }

    private void OnEnergyExhausted()
    {
        SetColor(_exhaustedColor);
    }

    private void OnEnergyRecovered()
    {
        SetColor(_normalColor);
    }

    private void SetColor(Color target)
    {
        _colorTween?.Kill();
        _colorTween = _fillImage.DOColor(target, _exhaustedTweenDuration).SetEase(Ease.OutCubic);
    }
}
