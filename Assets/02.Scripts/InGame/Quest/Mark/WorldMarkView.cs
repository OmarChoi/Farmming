using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class WorldMarkView : MonoBehaviour
{
    [Header("마크 오브젝트")]
    [SerializeField] private GameObject _availableMarkObject;
    [SerializeField] private GameObject _inProgressMarkObject;
    [SerializeField] private GameObject _canCompleteMarkObject;
    [SerializeField] private GameObject _updatedMarkObject;

    [Header("펄스 대상")]
    [SerializeField] private Transform _pulseTarget;

    [Header("펄스 옵션")]
    [SerializeField] private float _pulseScale = 1.1f;
    [SerializeField] private float _pulseDuration = 1.0f;

    private Tween _pulseTween;

    private readonly Dictionary<Transform, Vector3> _originalScales = new();


    private void Awake()
    {
        CacheOriginalScale(_pulseTarget);
        CacheOriginalScale(_availableMarkObject);
        CacheOriginalScale(_inProgressMarkObject);
        CacheOriginalScale(_canCompleteMarkObject);
        CacheOriginalScale(_updatedMarkObject);
    }

    private void OnDisable()
    {
        StopPulse();
    }

    public void ShowAvailable()
    {
        SetActiveMarks(true, false, false, false);
        PlayPulse(_availableMarkObject);
    }

    public void ShowInProgress()
    {
        SetActiveMarks(false, true, false, false);
        PlayPulse(_inProgressMarkObject);
    }

    public void ShowCanComplete()
    {
        SetActiveMarks(false, false, true, false);
        PlayPulse(_canCompleteMarkObject);
    }

    public void ShowUpdated()
    {
        SetActiveMarks(false, false, false, true);
        PlayPulse(_updatedMarkObject);
    }

    public void HideAll()
    {
        SetActiveMarks(false, false, false, false);
        StopPulse();
    }

    private void SetActiveMarks(bool showAvailable, bool showInProgress, bool showComplete, bool showUpdated)
    {
        if (_availableMarkObject != null)
        {
            _availableMarkObject.SetActive(showAvailable);
        }

        if (_inProgressMarkObject != null)
        {
            _inProgressMarkObject.SetActive(showInProgress);
        }

        if (_canCompleteMarkObject != null)
        {
            _canCompleteMarkObject.SetActive(showComplete);
        }

        if (_updatedMarkObject != null)
        {
            _updatedMarkObject.SetActive(showUpdated);
        }
    }

    private void PlayPulse(GameObject targetObject)
    {
        StopPulse();

        Transform target = _pulseTarget != null ? _pulseTarget : targetObject != null ? targetObject.transform : null;
        if (target == null) return;

        Vector3 baseScale = GetOriginalScale(target);
        target.localScale = baseScale;

        _pulseTween = target.DOScale(baseScale * _pulseScale, _pulseDuration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }

    private void StopPulse()
    {
        _pulseTween?.Kill();
        _pulseTween = null;

        ResetScale(_pulseTarget);
        ResetScale(_availableMarkObject);
        ResetScale(_inProgressMarkObject);
        ResetScale(_canCompleteMarkObject);
        ResetScale(_updatedMarkObject);
    }

    private void CacheOriginalScale(GameObject targetObject)
    {
        if (targetObject != null)
        {
            CacheOriginalScale(targetObject.transform);
        }
    }

    private void CacheOriginalScale(Transform target)
    {
        if (target == null) return;
        if (_originalScales.ContainsKey(target)) return;

        _originalScales[target] = target.localScale;
    }

    private Vector3 GetOriginalScale(Transform target)
    {
        if (target == null) return Vector3.one;

        if (_originalScales.TryGetValue(target, out Vector3 scale))
        {
            return scale;
        }

        _originalScales[target] = target.localScale;
        return target.localScale;
    }

    private void ResetScale(GameObject targetObject)
    {
        if (targetObject != null)
        {
            ResetScale(targetObject.transform);
        }
    }

    private void ResetScale(Transform target)
    {
        if (target == null) return;
        target.localScale = GetOriginalScale(target);
    }
}
