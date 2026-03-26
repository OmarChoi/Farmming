using System;
using UnityEngine;

public class IceVFX : MonoBehaviour
{
    [SerializeField] private GameObject _iceEffect;
    [SerializeField] private float _effectDuration = 1f;

    private Action _onLandCallback;

    public void Launch(Vector3 targetPosition, Vector3 direction, Action onLand = null)
    {
        _onLandCallback = onLand;

        if(direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        transform.position = targetPosition;
        OnLand(targetPosition);
    }

    private void OnLand(Vector3 position)
    {
        if(_iceEffect != null)
        {
            GameObject effect = Instantiate(_iceEffect, position, Quaternion.identity);
            Destroy(effect, _effectDuration);
        }
         _onLandCallback?.Invoke();
        Destroy(gameObject);
    }

}
