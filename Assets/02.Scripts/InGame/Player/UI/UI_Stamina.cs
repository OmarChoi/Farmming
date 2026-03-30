using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_Stamina : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private Color _warningColor = Color.red;
    [SerializeField, Range(0f, 1f)] private float _warningThreshold = 0.3f;
    [SerializeField] private float _tweenDuration = 0.3f;

    private PlayerStamina _stamina;
    private Color _defaultColor;
    private Tweener _fillTween;

    private void Awake()
    {
        _defaultColor = _fillImage.color;
        PlayerStaminaAbility.OnLocalPlayerReady += Bind;
    }

    private void OnDestroy()
    {
        _fillTween?.Kill();
        PlayerStaminaAbility.OnLocalPlayerReady -= Bind;
        Unbind();
    }

    private void Bind(PlayerStaminaAbility ability)
    {
        Unbind();

        _stamina = ability.Stamina;
        _stamina.OnChanged += UpdateFill;
        _stamina.OnMaxChanged += OnMaxChanged;
        UpdateFill(_stamina.Current);
    }

    private void Unbind()
    {
        if (_stamina == null) return;

        _stamina.OnChanged -= UpdateFill;
        _stamina.OnMaxChanged -= OnMaxChanged;
        _stamina = null;
    }

    private void OnMaxChanged(float _)
    {
        UpdateFill(_stamina.Current);
    }

    private void UpdateFill(float current)
    {
        if (_stamina == null || _stamina.Max <= 0f)
        {
            _fillImage.fillAmount = 0f;
            return;
        }

        float ratio = current / _stamina.Max;

        _fillTween?.Kill();
        _fillTween = _fillImage.DOFillAmount(ratio, _tweenDuration).SetEase(Ease.OutCubic);

        _fillImage.color = ratio <= _warningThreshold ? _warningColor : _defaultColor;
    }
}