using DG.Tweening;
using UnityEngine;

public class Wood : GatheringObject
{
    [Header("흔들림 연출")]
    [SerializeField] private float _shakeAngle = 5f;
    [SerializeField] private float _shakeDuration = 0.4f;
    [SerializeField] private int _shakeCount = 3;
    [SerializeField, Min(0f)] private float _breakSfxDelay = 0f;
    [SerializeField, Min(0f)] private float _breakSfxVolume = 2f;

    private Tween _shakeTween;
    private Tween _breakSfxTween;
    private Quaternion _originalRotation;
    private bool _breakSfxPlayed;

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
        PlayBreakSfxOnce();
        base.OnDepleted(info);
    }

    private void PlayBreakSfxOnce()
    {
        if (_breakSfxPlayed)
            return;

        _breakSfxPlayed = true;
        Vector3 breakSfxPosition = GetBreakSfxPosition();

        _breakSfxTween?.Kill();
        if (_breakSfxDelay > 0f)
        {
            _breakSfxTween = DOVirtual.DelayedCall(_breakSfxDelay, () =>
            {
                PlayBreakSfxAt(breakSfxPosition);
                _breakSfxTween = null;
            });
            return;
        }

        PlayBreakSfxAt(breakSfxPosition);
    }

    private void PlayBreakSfxAt(Vector3 position)
    {
        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.PlaySfx(new SfxPlayRequest(
            clipKey: AssetKey.SFX.WoodBreak,
            spatialMode: ESpatialMode.Positional3D,
            position: position,
            volume: _breakSfxVolume));
    }

    private Vector3 GetBreakSfxPosition()
    {
        Collider woodCollider = GetComponentInChildren<Collider>();
        if (woodCollider != null)
            return woodCollider.bounds.center;

        return transform.position;
    }
}
