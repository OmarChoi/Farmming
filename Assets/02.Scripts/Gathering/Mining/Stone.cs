using System.Collections;
using UnityEngine;

public class Stone : GatheringObject
{
    [Header("진동 연출")]
    [SerializeField] private float _shakeIntensity = 0.05f;
    [SerializeField] private float _shakeDuration = 0.2f;
    [SerializeField] private int _shakeCount = 6;

    private Coroutine _shakeCoroutine;
    private Vector3 _originalPosition;

    protected override void Init()
    {
        _originalPosition = transform.localPosition;
    }

    protected override void Hit()
    {
        // todo. 채광 연출 (파티클, 사운드)
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);

        _shakeCoroutine = StartCoroutine(Shake_Coroutine());
    }
    
    // todo. DOTween 기반으로 변경
    private IEnumerator Shake_Coroutine()
    {
        if (_shakeCount <= 0) yield break;
        float interval = _shakeDuration / _shakeCount;

        for (int i = 0; i < _shakeCount; i++)
        {
            float damping = 1f - (float)i / _shakeCount;
            Vector3 offset = Random.insideUnitSphere * (_shakeIntensity * damping);
            offset.y *= 0.3f;
            transform.localPosition = _originalPosition + offset;

            float t = 0f;
            while (t < interval)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        transform.localPosition = _originalPosition;
        _shakeCoroutine = null;
    }

    protected override void OnDepleted(GatheringInfo info)
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);

        // TODO: 부서지는 연출 (파티클, 사운드 등)
        base.OnDepleted(info);
    }
}
