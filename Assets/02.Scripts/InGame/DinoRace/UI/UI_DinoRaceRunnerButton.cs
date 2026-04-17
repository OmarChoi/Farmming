using ChocDino.UIFX;
using DG.Tweening;
using UnityEngine;

public class UI_DinoRaceRunnerButton : MonoBehaviour
{
    [SerializeField] private GlowFilter _glowFilter;
    [SerializeField] private RectTransform _imageTransform;
    [SerializeField] private float _glowDuration = 0.25f;
    [SerializeField] private float _selectedScale = 1.15f;
    [SerializeField] private float _selectDuration = 0.25f;

    private Tween _glowTween;
    private Tween _scaleTween;
    private bool _isSelected;

    private void Awake()
    {
        if (_glowFilter != null) _glowFilter.Strength = 0f;
        if (_imageTransform != null) _imageTransform.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        _glowTween?.Kill();
        _scaleTween?.Kill();
        if (_glowFilter != null) _glowFilter.Strength = 0f;
        if (_imageTransform != null) _imageTransform.localScale = Vector3.one;
        _isSelected = false;
    }

    public void SetSelected(bool selected)
    {
        if (_isSelected == selected) return;
        _isSelected = selected;

        FadeGlow(selected ? 1f : 0f);

        if (_imageTransform != null)
        {
            _scaleTween?.Kill();
            Vector3 target = selected ? Vector3.one * _selectedScale : Vector3.one;
            _scaleTween = _imageTransform.DOScale(target, _selectDuration)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);
        }
    }

    private void FadeGlow(float target)
    {
        if (_glowFilter == null) return;
        _glowTween?.Kill();
        _glowTween = DOTween.To(
                () => _glowFilter.Strength,
                v => _glowFilter.Strength = v,
                target,
                _glowDuration)
            .SetLink(gameObject);
    }
}