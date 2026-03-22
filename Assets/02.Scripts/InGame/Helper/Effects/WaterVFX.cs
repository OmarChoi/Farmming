using System.Collections;
using UnityEngine;

public class WaterVFX : MonoBehaviour
{
    [SerializeField] private GameObject _landEffectPrefab;

    public void PlayeEffect(Vector3 targetPosition, float duration)
    {
        transform.position = targetPosition;

        if(_landEffectPrefab != null )
        {
            GameObject effect = Instantiate(_landEffectPrefab, targetPosition, Quaternion.identity);
            Destroy(effect, duration);
        }

        Destroy(gameObject, duration);
    }
}
