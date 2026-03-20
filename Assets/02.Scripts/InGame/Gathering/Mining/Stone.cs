using DG.Tweening;
using UnityEngine;

public class Stone : GatheringObject
{
    [Header("진동 연출")]
    [SerializeField] private float _shakeIntensity = 0.05f;
    [SerializeField] private float _shakeDuration = 0.2f;
    [SerializeField] private int _shakeCount = 6;

    private Tween _shakeTween;
    private Vector3 _originalPosition;

    protected override void Init()
    {
        _originalPosition = transform.localPosition;
    }

    protected override void Hit()
    {
        _shakeTween?.Kill();
        transform.localPosition = _originalPosition;
        _shakeTween = transform.DOShakePosition(_shakeDuration, _shakeIntensity, _shakeCount, 90f, false, true, ShakeRandomnessMode.Harmonic)
                               .OnKill(() => transform.localPosition = _originalPosition);
    }

    protected override void OnDepleted(GatheringInfo info)
    {
        _shakeTween?.Kill();

        // TODO: 부서지는 연출 (파티클, 사운드 등)
        base.OnDepleted(info);
    }
}
