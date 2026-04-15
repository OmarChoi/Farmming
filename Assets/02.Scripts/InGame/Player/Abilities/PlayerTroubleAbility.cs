using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class PlayerTroubleAbility : PlayerAbility, ITroubleReceiver
{
    [Header("넉백 효과")]
    [SerializeField] private float _knockbackJumpHeight = 0.6f;

    private CharacterController _characterController;
    private PlayerAnimationAbility _animation;
    private PlayerMoveAbility _moveAbility;

    private float _minKnockbackDuration = 0.05f;

    private bool _isApplyingTrouble;

    private int _slowRequestId;

    private void Start()
    {
        _characterController = _owner.GetComponent<CharacterController>();
        _animation = _owner.GetAbility<PlayerAnimationAbility>();
        _moveAbility = _owner.GetAbility<PlayerMoveAbility>();
    }

    public bool CanReceiveTrouble()
    {
        return _owner != null && _owner.IsMine;
    }

    public void ApplyTrouble(TroubleContext context)
    {
        if (!CanReceiveTrouble()) return;

        switch (context.EffectType)
        {
            case ETroubleEffectType.Knockback:
                if (!_isApplyingTrouble)
                {
                    ApplyKnockbackAsync(context).Forget();
                }
                break;
            case ETroubleEffectType.Slow:
                ApplySlowAsync(context).Forget();
                break;
        }
    }

    private async UniTaskVoid ApplyKnockbackAsync(TroubleContext context)
    {
        _isApplyingTrouble = true;
        _owner.LockAction();

        if (_characterController != null)
        {
            _characterController.enabled = false;
        }

        Vector3 start = _owner.transform.position;
        Vector3 end = start + context.Direction * context.Power;

        Vector3 lookDirection = -context.Direction;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            _owner.transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        _animation?.PlayLavaHit();

        float elapsed = 0f;
        float duration = Mathf.Max(_minKnockbackDuration, context.Duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += Mathf.Sin(t * Mathf.PI) * _knockbackJumpHeight;

            _owner.transform.position = position;
            await UniTask.Yield();
        }

        _owner.transform.position = end;

        if (_characterController != null)
        {
            _characterController.enabled = true;
        }

        _owner.UnlockAction();
        _isApplyingTrouble = false;
    }

    private async UniTaskVoid ApplySlowAsync(TroubleContext context)
    {
        if (_moveAbility == null) return;

        _slowRequestId++;
        int requestId = _slowRequestId;

        float slowMultiplier = 1f - Mathf.Clamp(context.Power, 0f, 1f);
        _moveAbility.SetExternalMoveSpeedMultiplier(slowMultiplier);

        try
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(context.Duration),
                cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (requestId != _slowRequestId) return;

        _moveAbility.ClearExternalMoveSpeedMultiplier();
    }

    private void OnDisable()
    {
        if (_moveAbility != null)
        {
            _moveAbility.ClearExternalMoveSpeedMultiplier();
        }
    }
}
