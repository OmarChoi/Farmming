using System;
using DG.Tweening;
using UnityEngine;

public class HelperSummonVfxAbility : HelperAbility
{
    [SerializeField] private GameObject _summonEffectPrefab;
    [SerializeField] private Vector3 _effectOffset = Vector3.zero;
    [SerializeField] private float _scaleDuration = 0.5f;
    [SerializeField] private Ease _scaleEase = Ease.OutBack;
    [SerializeField] private float _despawnDuration = 0.35f;
    [SerializeField] private Ease _despawnEase = Ease.InBack;

    private GameObject _effectInstance;
    private ParticleSystem[] _particleSystems;
    private Tween _scaleTween;

    public void PlayOnSummon()
    {
        EnsureEffectCreated();
        PlayEffect();
        PlayScaleTween();
    }

    public void StopScaleTween()
    {
        _scaleTween?.Kill();
        _scaleTween = null;
    }

    private void EnsureEffectCreated()
    {
        if (_effectInstance != null) return;
        if (_summonEffectPrefab == null) return;

        _effectInstance = Instantiate(_summonEffectPrefab);
        _particleSystems = _effectInstance.GetComponentsInChildren<ParticleSystem>(true);
        _effectInstance.SetActive(false);
    }

    private void PlayEffect()
    {
        if (_effectInstance == null) return;

        _effectInstance.transform.position = _owner.transform.position + _effectOffset;
        _effectInstance.transform.rotation = _owner.transform.rotation;
        _effectInstance.SetActive(true);

        if (_particleSystems == null) return;
        for (int i = 0; i < _particleSystems.Length; i++)
        {
            ParticleSystem ps = _particleSystems[i];
            if (ps == null) continue;
            ps.Clear(true);
            ps.Play(true);
        }
    }

    private void PlayScaleTween()
    {
        _scaleTween?.Kill();
        _owner.transform.localScale = Vector3.zero;
        _scaleTween = _owner.transform
            .DOScale(_owner.OriginalScale, _scaleDuration)
            .SetEase(_scaleEase);
    }

    public void PlayOnDespawn(Action onComplete)
    {
        StopScaleTween();

        if (_owner == null)
        {
            onComplete?.Invoke();
            return;
        }

        bool invoked = false;
        Action safeInvoke = () =>
        {
            if (invoked) return;
            invoked = true;
            onComplete?.Invoke();
        };

        _scaleTween = _owner.transform
            .DOScale(Vector3.zero, _despawnDuration)
            .SetEase(_despawnEase)
            .OnComplete(() => safeInvoke())
            .OnKill(() => safeInvoke());
    }

    private void OnDestroy()
    {
        StopScaleTween();
        if (_effectInstance != null)
            Destroy(_effectInstance);
    }
}
