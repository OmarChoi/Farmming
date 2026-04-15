using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class CaveTroublemakerBehaviour : TroublemakerBehaviourBase
{
    [Header("방해 범위 판정")]
    [SerializeField] private LayerMask _playerLayerMask;
    [SerializeField] private int _overlapBufferSize = 8;

    [Header("방해 효과 연출")]
    [SerializeField] private GameObject _troubleEffectPrefab;
    [SerializeField] private Vector3 _effectOffset = Vector3.zero;

    private Collider[] _hits;
    private bool _isTroubling;
    private float _lastTroubleTime = -999f;

    private void Awake()
    {
        _hits = new Collider[Mathf.Max(1, _overlapBufferSize)];
    }

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

        try
        {
            if (target != null && Controller.Movement != null)
            {
                await Controller.Movement.FaceTargetAsync(target.position);
            }

            Controller.PlayTroubleAll();

            await UniTask.Delay(
                TimeSpan.FromSeconds(Controller.Data.TroubleHitDelay),
                cancellationToken: this.GetCancellationTokenOnDestroy());

            PlayTroubleEffect();
            bool applied = ApplySlowInRange();

            _lastTroubleTime = Time.time;

            if (applied)
            {
                ClearTargetAndReturnHome();
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _isTroubling = false;
        }
    }

    private bool ApplySlowInRange()
    {
        if (Controller == null || Controller.Data == null) return false;

        int hitCount = Physics.OverlapSphereNonAlloc(
            Controller.transform.position,
            Controller.Data.TroubleRange,
            _hits,
            _playerLayerMask,
            QueryTriggerInteraction.Ignore);

        bool appliedAtLeastOne = false;
        HashSet<PlayerController> uniquePlayers = new();

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _hits[i];
            if (hit == null) continue;

            PlayerController player = hit.GetComponentInParent<PlayerController>();
            if (player == null || player.PhotonView == null) continue;
            if (!uniquePlayers.Add(player)) continue;

            ApplySlow(player);
            appliedAtLeastOne = true;
        }

        return appliedAtLeastOne;
    }

    private void ApplySlow(PlayerController player)
    {
        if (player == null || player.PhotonView == null || Controller == null || Controller.Data == null) return;

        Vector3 direction = player.transform.position - Controller.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = player.transform.forward;
        }

        direction.Normalize();

        player.RequestTrouble(
            Controller.Data.TroubleEffectType,
            Vector3.zero,
            Controller.Data.TroublePower,
            Controller.Data.TroubleDuration);
    }

    private void PlayTroubleEffect()
    {
        if (_troubleEffectPrefab == null) return;

        Vector3 spawnPos = Controller.transform.position + _effectOffset;
        Instantiate(_troubleEffectPrefab, spawnPos, Quaternion.identity);
    }

    private void ClearTargetAndReturnHome()
    {
        Controller.ClearTarget();
    }
}
