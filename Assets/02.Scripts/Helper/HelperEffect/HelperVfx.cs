using System;
using UnityEngine;

public class HelperVfx : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private GameObject _woodCuttingEffectPrefab;
    [SerializeField] private Transform _effectSpawnPoint;
    [SerializeField] private float _effectSeeTime = 2f;

    private Vector3 _lastTargetPosition;

    public event Action OnWoodCuttingHitEvent;

    public void PlayChop(Vector3 targetPosition)
    {
        _lastTargetPosition = targetPosition;

        if (_animator != null)
        {
            _animator.SetTrigger("WoodCutting");
        }
        else
        {
            TriggerChopHit(targetPosition);
        }
    }

    public void TriggerChopHit(Vector3 targetPosition)
    {
        if(_woodCuttingEffectPrefab != null)
        {
            Vector3 spawnPos = _effectSpawnPoint != null ? _effectSpawnPoint.position : transform.position;
            GameObject effect = Instantiate(_woodCuttingEffectPrefab, spawnPos, Quaternion.identity);

            Vector3 direction = (targetPosition - spawnPos).normalized;
            if(direction != Vector3.zero)
            {
                effect.transform.forward = direction;
            }

            Destroy(effect, _effectSeeTime);
        }

        OnWoodCuttingHitEvent?.Invoke();
    }
}
