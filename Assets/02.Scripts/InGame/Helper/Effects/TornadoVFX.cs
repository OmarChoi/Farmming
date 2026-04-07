using System;
using System.Collections;
using UnityEngine;

public class TornadoVFX : MonoBehaviour
{
    [SerializeField] private float _sweepDuration = 2f;
    [SerializeField] private float _sweepDistance = 1.5f;
    [SerializeField] private float _fadeOutDuration = 0.5f;

    public void Initialize(Vector3 rightDirection, float sweepDistance, float sweepDuration, Action onComplete = null)
    {
        _sweepDistance = sweepDistance;
        _sweepDuration = sweepDuration;
        StartCoroutine(SweepCoroutine(rightDirection, onComplete));
    }

    private IEnumerator SweepCoroutine(Vector3 rightDir, Action onComplete)
    {
        Vector3 center = transform.position;
        Vector3 leftPos = center - rightDir * _sweepDistance;
        Vector3 rightPos = center + rightDir * _sweepDistance;

        float halfDuration = _sweepDuration * 0.5f;

        // 가운데 → 왼쪽
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            transform.position = Vector3.Lerp(center, leftPos, t);
            yield return null;
        }

        // 왼쪽 → 오른쪽
        elapsed = 0f;
        while (elapsed < _sweepDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _sweepDuration);
            transform.position = Vector3.Lerp(leftPos, rightPos, t);
            yield return null;
        }

        // 파티클 자연스럽게 종료
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>())
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        yield return new WaitForSeconds(_fadeOutDuration);

        onComplete?.Invoke();
        Destroy(gameObject);
    }
}
