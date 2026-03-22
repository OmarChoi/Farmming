using System.Collections;
using UnityEngine;

public class WaterVFX : MonoBehaviour
{
    [SerializeField] private GameObject _landEffectPrefab;
    [SerializeField] private float _effectDuration = 0.3f;

    public void OnLand(Vector3 targetPosition)
    {
        transform.position = targetPosition;
        StartCoroutine(LandCoroutine(targetPosition));
    }

    private IEnumerator LandCoroutine(Vector3 targetPosition)
    {
        yield return null;

        if (_landEffectPrefab != null)
        {
            GameObject effect = Instantiate(_landEffectPrefab, targetPosition, Quaternion.identity);
            Destroy(effect, _effectDuration);
        }

        Destroy(gameObject);
    }
}
