using System;
using TMPro;
using UnityEngine;

public class IceVFX : MonoBehaviour
{
    [SerializeField] private GameObject _iceEffect;
    [SerializeField] private float _effectDuration = 3f;

    private Action _onLandCallback;
    private bool _landing = false;

    public void Launch(Vector3 targetPosition, Vector3 direction, Action onLand = null)
    {
        _onLandCallback = onLand;

        if(direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

       //transform.position = targetPosition;
    }

    private void OnLand(Vector3 position)
    {
        _onLandCallback?.Invoke(); 
        Destroy(gameObject, _effectDuration);
    }

    private void OnParticleCollision(GameObject other)
    {
        if (!_landing)
        {
            _landing = true;
            _onLandCallback?.Invoke();
            Destroy(gameObject, _effectDuration);
        }
    }
}
