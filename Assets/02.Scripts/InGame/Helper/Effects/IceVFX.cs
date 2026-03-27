using System;
using UnityEngine;

public class IceVFX : MonoBehaviour
{
    [SerializeField] private SmokeEffect _iceSmokePrefab;
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

        if (_iceSmokePrefab != null)
        {
            Vector3 hitPos = other.transform.position + Vector3.up * 0.1f;
            SmokeEffect effect = Instantiate(_iceSmokePrefab, hitPos, Quaternion.identity);
            effect.StartSmokeEffect();
        }

        _onLandCallback?.Invoke();

        Destroy(gameObject, _effectDuration);
    }
}
