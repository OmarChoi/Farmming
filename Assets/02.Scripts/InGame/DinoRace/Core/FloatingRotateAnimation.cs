using DG.Tweening;
using UnityEngine;

public class FloatingRotateAnimation : MonoBehaviour
{
    [Header("Bob")]
    [SerializeField] private float _bobAmplitude = 0.15f;
    [SerializeField] private float _bobDuration = 1f;
    [SerializeField] private Ease _bobEase = Ease.InOutSine;

    [Header("Rotate")]
    [SerializeField] private Vector3 _rotationAxis = new Vector3(0f, 1f, 0f);
    [SerializeField] private float _rotationDuration = 2f;
    [SerializeField] private Ease _rotationEase = Ease.Linear;

    private Vector3 _basePosition;
    private Vector3 _baseEulerAngles;
    private Tween _bobTween;
    private Tween _rotateTween;

    private void OnEnable()
    {
        _basePosition = transform.localPosition;
        _baseEulerAngles = transform.localEulerAngles;
        StartBob();
        StartRotate();
    }

    private void OnDisable()
    {
        _bobTween?.Kill();
        _rotateTween?.Kill();
        _bobTween = null;
        _rotateTween = null;

        transform.localPosition = _basePosition;
        transform.localEulerAngles = _baseEulerAngles;
    }

    private void StartBob()
    {
        if (_bobAmplitude <= 0f || _bobDuration <= 0f) return;

        Vector3 low = _basePosition + Vector3.down * _bobAmplitude;
        Vector3 high = _basePosition + Vector3.up * _bobAmplitude;

        transform.localPosition = low;
        _bobTween = transform.DOLocalMove(high, _bobDuration)
            .SetEase(_bobEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StartRotate()
    {
        if (_rotationDuration <= 0f || _rotationAxis.sqrMagnitude <= 0.0001f) return;

        Vector3 delta = _rotationAxis.normalized * 360f;
        _rotateTween = transform.DOLocalRotate(delta, _rotationDuration, RotateMode.FastBeyond360)
            .SetEase(_rotationEase)
            .SetLoops(-1, LoopType.Incremental);
    }
}