using System;
using TMPro;
using UnityEngine;

public class IceVFX : MonoBehaviour
{
    [SerializeField] private GameObject _iceEffect;
    [SerializeField] private float _effectDuration = 3f;

    private Action _onLandCallback;
    private bool _landed = false;

    public void Launch(Vector3 targetPosition, Vector3 direction, Action onLand = null)
    {
        _onLandCallback = onLand;

        if(direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

    }

    private void OnParticleCollision(GameObject other)
    {
        if (_landed) return;
        _landed = true;

        if (_iceEffect != null)
        {
            Vector3 hitPos = other.transform.position + Vector3.up * 0.1f;
            GameObject effect = Instantiate(_iceEffect, hitPos, Quaternion.identity);
            Destroy(effect, _effectDuration);
        }

        _onLandCallback?.Invoke();

        Destroy(gameObject, _effectDuration);
    }
}
