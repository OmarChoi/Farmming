using System.Collections;
using UnityEngine;

public class Wood : GatheringObject
{
    [Header("흔들림 연출")]
    [SerializeField] private float _shakeAngle = 5f;
    [SerializeField] private float _shakeDuration = 0.4f;
    [SerializeField] private int _shakeCount = 3;

    private Vector3 _centralAxis;
    private Coroutine _shakeCoroutine;
    private Quaternion _originalRotation;

    protected override void Init()
    {
        _originalRotation = transform.rotation;
    }

    protected override void Hit()
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _centralAxis = UnityEngine.Random.onUnitSphere;
        _shakeCoroutine = StartCoroutine(Shake_Coroutine());
    }

    // todo. DOTween 기반으로 수정
    private IEnumerator Shake_Coroutine()
    {
        if (_shakeCount <= 0) yield break;
        float halfInterval = _shakeDuration / (_shakeCount * 2);
        Vector3 axis = transform.TransformDirection(_centralAxis);

        for (int i = 0; i < _shakeCount; i++)
        {
            float damping = 1f - (float)i / _shakeCount;
            float currentAngle = _shakeAngle * damping;

            // 한쪽으로
            float t = 0f;
            while (t < halfInterval)
            {
                float angle = Mathf.Sin(t / halfInterval * Mathf.PI * 0.5f) * currentAngle;
                transform.rotation = _originalRotation * Quaternion.AngleAxis(angle, axis);
                t += Time.deltaTime;
                yield return null;
            }

            // 반대쪽으로
            t = 0f;
            while (t < halfInterval)
            {
                float angle = Mathf.Lerp(currentAngle, -currentAngle, t / halfInterval);
                transform.rotation = _originalRotation * Quaternion.AngleAxis(angle, axis);
                t += Time.deltaTime;
                yield return null;
            }
        }

        transform.rotation = _originalRotation;
        _shakeCoroutine = null;
    }

    protected override void OnDepleted()
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        // TODO: 나무 벌목 연출 (파티클, 사운드 등)
        base.OnDepleted();
    }
}
