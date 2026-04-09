using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SowSecondaryVFXAbility : HelperAbility
{
    [Header("Secondary")]
    [SerializeField] private float _floatAbovePlayerHeight = 3f;
    [SerializeField] private float _floatMoveDuration = 0.5f;
    [SerializeField] private float _floatDuration = 2f;
    [SerializeField] private float _bobAmplitude = 0.3f;
    [SerializeField] private float _bobDuration = 0.4f;
    [SerializeField] private float _returnDuration = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float _legendaryHoldNormalizedTime = 0.35f;
    [SerializeField] private float _legendaryHoldDuration = 1.5f;
    [SerializeField] [Range(0f, 1f)] private float _vfxTriggerNormalizedTime = 0.65f;

    private HelperAnimationAbility _animationAbility;
    private HelperFollowAbility _followAbility;
    private SowEpicSecondaryVFXAbility _epicSecondaryVfx;
    private SowLegendarySecondaryVFXAbility _legendarySecondaryVfx;

    private Coroutine _presentationCoroutine;
    private Coroutine _legendaryHoldCoroutine;

    protected override void Awake()
    {
        base.Awake();
        _animationAbility = _owner.GetAbility<HelperAnimationAbility>();
        _followAbility = _owner.GetAbility<HelperFollowAbility>();
        _epicSecondaryVfx = _owner.GetAbility<SowEpicSecondaryVFXAbility>();
        _legendarySecondaryVfx = _owner.GetAbility<SowLegendarySecondaryVFXAbility>();
    }

    public void PlayEpic(
        Transform mouthPoint,
        List<TerrainCell> orderedCells,
        Action<TerrainCell> onCellLand,
        Action onComplete)
    {
        StartPresentation(
            effectsComplete => PlayEpicEffects(mouthPoint, orderedCells, onCellLand, effectsComplete),
            onComplete);
    }

    public void PlayLegendary(
        Transform mouthPoint,
        List<TerrainCell> orderedCells,
        Action<TerrainCell> onCellLand,
        Action onComplete)
    {
        StartPresentation(
            effectsComplete =>
            {
                PlayLegendaryAnimation();
                PlayLegendaryEffects(mouthPoint, orderedCells, onCellLand, effectsComplete);
            },
            onComplete);
    }

    public void ReplayEpicSow()
    {
        StartCoroutine(ReplayEpicSowAndWait());
    }

    public IEnumerator ReplayEpicSowAndWait()
    {
        if (_animationAbility == null)
            yield break;

        yield return StartCoroutine(
            _animationAbility.ForceReplayAndWait(EHelperAnim.EpicSow, _vfxTriggerNormalizedTime));
    }

    public void CancelPresentation()
    {
        if (_presentationCoroutine != null)
        {
            StopCoroutine(_presentationCoroutine);
            _presentationCoroutine = null;
        }

        StopLegendaryHoldRoutine();
        ResetAnimatorSpeed();
        _owner?.transform.DOKill();

        if (_followAbility != null)
            _followAbility.enabled = true;
    }

    private void StartPresentation(
        Action<Action> startEffects,
        Action onComplete)
    {
        CancelPresentation();
        _presentationCoroutine = StartCoroutine(FloatAndPresent(startEffects, onComplete));
    }

    private IEnumerator FloatAndPresent(
        Action<Action> startEffects,
        Action onComplete)
    {
        if (_owner.PlayerOwner == null)
        {
            _presentationCoroutine = null;
            onComplete?.Invoke();
            yield break;
        }

        if (_followAbility != null)
            _followAbility.enabled = false;

        Vector3 originalPosition = _owner.transform.position;
        Vector3 floatPosition = _owner.PlayerOwner.transform.position + Vector3.up * _floatAbovePlayerHeight;
        Tween floatTween = _owner.transform.DOMove(floatPosition, _floatMoveDuration)
            .SetEase(Ease.OutQuad);

        bool finished = false;
        startEffects?.Invoke(() => finished = true);

        yield return floatTween.WaitForCompletion();

        Tween bobTween = null;
        if (!finished || _floatDuration > 0f)
        {
            bobTween = _owner.transform.DOMoveY(floatPosition.y + _bobAmplitude, _bobDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        float minimumFloatEndTime = Time.time + Mathf.Max(0f, _floatDuration);
        yield return new WaitUntil(() => finished && Time.time >= minimumFloatEndTime);

        bobTween?.Kill();

        yield return _owner.transform.DOMove(originalPosition, _returnDuration)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();

        if (_followAbility != null)
            _followAbility.enabled = true;

        _presentationCoroutine = null;
        onComplete?.Invoke();
    }

    private void PlayEpicEffects(
        Transform mouthPoint,
        List<TerrainCell> orderedCells,
        Action<TerrainCell> onCellLand,
        Action onEffectsComplete)
    {
        if (_epicSecondaryVfx == null)
        {
            InvokeOnCells(orderedCells, onCellLand);
            onEffectsComplete?.Invoke();
            return;
        }

        _epicSecondaryVfx.SpawnEffects(
            mouthPoint,
            orderedCells,
            onCellLand,
            onEffectsComplete,
            ReplayEpicSowAndWait);
    }

    private void PlayLegendaryEffects(
        Transform mouthPoint,
        List<TerrainCell> orderedCells,
        Action<TerrainCell> onCellLand,
        Action onEffectsComplete)
    {
        if (_legendarySecondaryVfx == null)
        {
            InvokeOnCells(orderedCells, onCellLand);
            onEffectsComplete?.Invoke();
            return;
        }

        _legendarySecondaryVfx.SpawnEffects(mouthPoint, orderedCells, onCellLand, onEffectsComplete);
    }

    private void PlayLegendaryAnimation()
    {
        ResetAnimatorSpeed();
        StopLegendaryHoldRoutine();

        if (_animationAbility == null)
            return;

        if (_legendaryHoldDuration <= 0f)
        {
            _animationAbility.Replay(EHelperAnim.LegendarySow);
            return;
        }

        _legendaryHoldCoroutine = StartCoroutine(ForceReplayAndHoldLegendarySow());
    }

    private IEnumerator ForceReplayAndHoldLegendarySow()
    {
        yield return StartCoroutine(
            _animationAbility.ForceReplayAndWait(EHelperAnim.LegendarySow, _legendaryHoldNormalizedTime));

        Animator animator = _animationAbility?.Animator;
        if (animator == null)
        {
            _legendaryHoldCoroutine = null;
            yield break;
        }

        animator.speed = 0f;
        yield return new WaitForSeconds(_legendaryHoldDuration);

        animator = _animationAbility?.Animator;
        if (animator != null)
            animator.speed = 1f;

        _legendaryHoldCoroutine = null;
    }

    private void StopLegendaryHoldRoutine()
    {
        if (_legendaryHoldCoroutine == null)
            return;

        StopCoroutine(_legendaryHoldCoroutine);
        _legendaryHoldCoroutine = null;
    }

    private void ResetAnimatorSpeed()
    {
        Animator animator = _animationAbility?.Animator;
        if (animator != null)
            animator.speed = 1f;
    }

    private static void InvokeOnCells(List<TerrainCell> orderedCells, Action<TerrainCell> onCellLand)
    {
        if (orderedCells == null)
            return;

        foreach (TerrainCell cell in orderedCells)
            onCellLand?.Invoke(cell);
    }

    private void OnDisable()
    {
        CancelPresentation();
    }
}
