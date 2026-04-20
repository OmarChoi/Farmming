using UnityEngine;
using DG.Tweening;

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

    private void SetActiveMarks(bool showAvailable, bool showInProgress, bool showComplete, bool showRefresh)
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
            _updatedMarkObject.SetActive(showRefresh);
        }
    }

    private void PlayPulse(GameObject targetObject)
    {
        StopPulse();

        Transform target = _pulseTarget != null ? _pulseTarget : targetObject != null ? targetObject.transform : null;
        if (target == null) return;

        target.localScale = Vector3.one;

        _pulseTween = target.DOScale(_pulseScale, _pulseDuration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
    }

    private void StopPulse()
    {
        _pulseTween?.Kill();
        _pulseTween = null;

        if (_pulseTarget != null)
        {
            _pulseTarget.localScale = Vector3.one;
        }

        ResetScale(_availableMarkObject);
        ResetScale(_inProgressMarkObject);
        ResetScale(_canCompleteMarkObject);
        ResetScale(_updatedMarkObject);
    }

    private void ResetScale(GameObject targetObject)
    {
        if (targetObject != null)
        {
            targetObject.transform.localScale = Vector3.one;
        }
    }
}
