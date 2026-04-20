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
        if (_characterController == null) return;

        _isApplyingTrouble = true;
        _owner.LockAction();

        Vector3 start = _owner.transform.position;

        Vector3 flatDirection = context.Direction;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.001f)
        {
            flatDirection = _owner.transform.forward;
        }

        flatDirection.Normalize();

        Vector3 end = start + flatDirection * context.Power;

        Vector3 lookDirection = -flatDirection;
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

            Vector3 target = Vector3.Lerp(start, end, t);
            target.y += Mathf.Sin(t * Mathf.PI) * _knockbackJumpHeight;

            Vector3 delta = target - _owner.transform.position;
            _characterController.Move(delta);

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        SnapToGround();

        _owner.UnlockAction();
        _isApplyingTrouble = false;
    }

    private void SnapToGround()
    {
        if (_characterController == null) return;

        Vector3 origin = _owner.transform.position + Vector3.up * 1.0f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 3f, ~0, QueryTriggerInteraction.Ignore))
        {
            float safeY = hit.point.y + _characterController.skinWidth;
            Vector3 position = _owner.transform.position;

            if (position.y < safeY)
            {
                Vector3 correction = new Vector3(0f, safeY - position.y, 0f);
                _characterController.Move(correction);
            }
        }
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
