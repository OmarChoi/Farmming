using DG.Tweening;
using UnityEngine;

public class SowVFX : MonoBehaviour
{
    [SerializeField] private float _arcHeight = 1f;
    [SerializeField] private float _duration = 0.5f;
    [SerializeField] private GameObject _landEffectPrefab;
    [SerializeField] private int _jumpCount = 1;
    [SerializeField] private float _effectDuration = 1f;

    public void Launch(Vector3 targetPosition)
    {
        transform.DOJump(targetPosition, _arcHeight, _jumpCount, _duration)
                 .SetEase(Ease.Linear)
                 .OnComplete(() => OnLand(targetPosition));
    }

    private void OnLand(Vector3 position)
    {
        if(_landEffectPrefab != null)
        {
            GameObject effect = Instantiate(_landEffectPrefab, position, Quaternion.identity);
            Destroy(effect, _effectDuration);
        }

        Destroy(gameObject);
    }
}
