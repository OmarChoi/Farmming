using DG.Tweening;
using UnityEngine;

public class Wood : GatheringObject
{
    [Header("흔들림 연출")]
    [SerializeField] private float _shakeAngle = 5f;
    [SerializeField] private float _shakeDuration = 0.4f;
    [SerializeField] private int _shakeCount = 3;

    private Tween _shakeTween;
    private Quaternion _originalRotation;

    protected override void Init()
    {
        _originalRotation = transform.rotation;
    }

    protected override void Hit()
    {
        _shakeTween?.Kill();
        Vector3 axis = transform.TransformDirection(Random.onUnitSphere);
        Vector3 shakeStrength = axis * _shakeAngle;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySfx(new SfxPlayRequest(
                clipKey: AssetKey.SFX.WoodLeaf,
                spatialMode: ESpatialMode.Positional3D,
                position: transform.position));
        }

        transform.rotation = _originalRotation;
        _shakeTween = transform.DOShakeRotation(_shakeDuration, shakeStrength, _shakeCount, 90f, true, ShakeRandomnessMode.Harmonic)
                               .OnKill(() => transform.rotation = _originalRotation);
    }

    protected override void OnDepleted(GatheringInfo info)
    {
        _shakeTween?.Kill();
        // TODO: 나무 벌목 연출 (파티클, 사운드 등)
        base.OnDepleted(info);
    }
}
