using System;
using DG.Tweening;
using UnityEngine;

public class WaterVFX : MonoBehaviour
{
    [SerializeField] private GameObject _splashEffectPrefab; // 물터지는 이펙트
    [SerializeField] private float _arcHeight = 3f;
    [SerializeField] private float _duration = 0.5f;
    [SerializeField] private int _jumpCount = 1;
    [SerializeField] private float _splashDuration = 1f;

    public void Launch(Vector3 targetPosition, Vector3 direction, Action onLand = null)
    {
        // 발사 방향으로 회전
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        // 포물선 이동
        transform.DOJump(targetPosition, _arcHeight, _jumpCount, _duration)
                 .SetEase(Ease.Linear)
                 .OnComplete(() => OnLand(targetPosition, onLand));
    }

    private void OnLand(Vector3 position, Action onLand)
    {
        if (_splashEffectPrefab != null)
        {
            GameObject splash = Instantiate(_splashEffectPrefab, position, Quaternion.identity);
            Destroy(splash, _splashDuration);
        }

        onLand?.Invoke();

        Destroy(gameObject);
    }
}
