using System.Collections;
using UnityEngine;

public class WaterVFX : MonoBehaviour
{
    [SerializeField] private GameObject _landEffectPrefab;
    [SerializeField] private float _duration = 0.3f;

    public void PlayeEffect(Vector3 targetPosition, Vector3 direction)
    {
        transform.position = targetPosition;

        if(direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        if(_landEffectPrefab != null )
        {
            GameObject effect = Instantiate(_landEffectPrefab, targetPosition, transform.rotation);
            Destroy(effect, _duration);
        }

        Destroy(gameObject, _duration);
    }
}
