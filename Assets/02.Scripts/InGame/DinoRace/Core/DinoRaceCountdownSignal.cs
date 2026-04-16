using System;
using UnityEngine;
using DG.Tweening;

public class DinoRaceCountdownSignal : MonoBehaviour
{
    [SerializeField] private Transform[] _signalDinos;
    [SerializeField] private float _initialDelay = 0.5f;
    [SerializeField] private float _intervalBetweenSignals = 0.8f;
    [SerializeField] private float _scaleDuration = 0.4f;
    [SerializeField] private float _targetScale = 0.6f;
    [SerializeField] private float _hideDelay = 3f;
    [SerializeField] private float _shrinkDuration = 0.3f;

    private static readonly int AnimationParam = Animator.StringToHash("animation");
    private const int FlyAnimationValue = 17;

    private Sequence _sequence;
    private Sequence _hideSequence;

    public float TotalDuration =>
        _initialDelay
        + Mathf.Max(0, _signalDinos.Length - 1) * _intervalBetweenSignals
        + _scaleDuration;

    public void Play(Action onComplete)
    {
        Stop();

        for (int i = 0; i < _signalDinos.Length; i++)
        {
            if (_signalDinos[i] == null) continue;
            _signalDinos[i].gameObject.SetActive(false);
            _signalDinos[i].localScale = Vector3.zero;
        }

        _sequence = DOTween.Sequence();
        _sequence.AppendInterval(_initialDelay);

        for (int i = 0; i < _signalDinos.Length; i++)
        {
            if (_signalDinos[i] == null) continue;
            int idx = i;

            if (i > 0)
                _sequence.AppendInterval(_intervalBetweenSignals);

            _sequence.AppendCallback(() =>
            {
                _signalDinos[idx].gameObject.SetActive(true);
                Animator anim = _signalDinos[idx].GetComponent<Animator>();
                if (anim != null)
                    anim.SetInteger(AnimationParam, FlyAnimationValue);
            });
            _sequence.Append(
                _signalDinos[i].DOScale(_targetScale, _scaleDuration)
                    .SetEase(Ease.OutBack));
        }

        _sequence.OnComplete(() =>
        {
            onComplete?.Invoke();
            PlayHide();
        });
        _sequence.Play();
    }

    private void PlayHide()
    {
        _hideSequence?.Kill();
        _hideSequence = DOTween.Sequence();
        _hideSequence.AppendInterval(_hideDelay);

        bool first = true;
        for (int i = 0; i < _signalDinos.Length; i++)
        {
            if (_signalDinos[i] == null) continue;
            int idx = i;

            var tween = _signalDinos[i].DOScale(0f, _shrinkDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() => _signalDinos[idx].gameObject.SetActive(false));

            if (first)
            {
                _hideSequence.Append(tween);
                first = false;
            }
            else
            {
                _hideSequence.Join(tween);
            }
        }

        _hideSequence.Play();
    }

    public void Stop()
    {
        _sequence?.Kill();
        _sequence = null;
        _hideSequence?.Kill();
        _hideSequence = null;
    }

    public void Hide()
    {
        Stop();
        for (int i = 0; i < _signalDinos.Length; i++)
        {
            if (_signalDinos[i] == null) continue;
            _signalDinos[i].localScale = Vector3.zero;
            _signalDinos[i].gameObject.SetActive(false);
        }
    }
}