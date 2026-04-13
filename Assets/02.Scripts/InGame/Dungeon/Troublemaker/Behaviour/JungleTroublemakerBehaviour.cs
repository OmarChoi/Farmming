using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

public class JungleTroublemakerBehaviour : TroublemakerBehaviourBase
{
    private bool _isTroubling;
    private float _lastTroubleTime = -999f;

    public override void OnReachTarget(Transform target)
    {
        if (target == null || _isTroubling || Controller == null || Controller.Data == null) return;
        if (Time.time < _lastTroubleTime + Controller.Data.TroubleCooldown) return;

        TryTroubleAsync(target).Forget();
    }

    private async UniTaskVoid TryTroubleAsync(Transform target)
    {
        _isTroubling = true;

        Controller.Movement?.Stop();

        if (target != null)
        {
            await Controller.Movement.FaceTargetAsync(target.position);
        }

        Controller.Anim?.PlayTrouble();

        await UniTask.Delay(
            TimeSpan.FromSeconds(Controller.Data.TroubleHitDelay),
            cancellationToken: this.GetCancellationTokenOnDestroy());

        if (target != null)
        {
            float distance = Vector3.Distance(Controller.transform.position, target.position);
            if (distance <= Controller.Data.TroubleRange)
            {
                ApplyTrouble(target);
            }
        }

        _lastTroubleTime = Time.time;
        _isTroubling = false;
    }

    private void ApplyTrouble(Transform target)
    {
        ITroubleReceiver receiver = target.GetComponentInParent<ITroubleReceiver>();
        if (receiver == null || !receiver.CanReceiveTrouble()) return;

        Vector3 direction = target.position - Controller.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = target.forward;
        }

        TroubleContext context = new TroubleContext
        {
            Source = Controller,
            EffectType = Controller.Data.TroubleEffectType,
            Direction = direction.normalized,
            Power = Controller.Data.TroublePower,
            Duration = Controller.Data.TroubleDuration
        };

        receiver.ApplyTrouble(context);
    }
}
