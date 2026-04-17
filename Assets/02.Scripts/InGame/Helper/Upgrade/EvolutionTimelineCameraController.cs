using UnityEngine;
using UnityEngine.Playables;

public class EvolutionTimelineCameraController : MonoBehaviour
{
    [Header("Runtime References")]
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _orbitPivot;

    [Header("Curves")]
    [SerializeField] private AnimationCurve _impactZoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve _smoothScanCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private HelperEvolutionProfileSO _profile;
    private bool _isPlaying;

    public void Configure(
        HelperEvolutionProfileSO profile,
        PlayableDirector director,
        Transform cameraTransform,
        Transform orbitPivot)
    {
        _profile = profile;
        _director = director;
        _cameraTransform = cameraTransform;
        _orbitPivot = orbitPivot;
    }

    public void Play()
    {
        _isPlaying = _profile != null && _cameraTransform != null && _orbitPivot != null;
        if (_isPlaying)
            ApplyAtTime(0f);
    }

    public void Stop()
    {
        _isPlaying = false;
    }

    private void LateUpdate()
    {
        if (!_isPlaying) return;

        float time = _director != null
            ? (float)_director.time
            : Time.time;

        ApplyAtTime(time);
    }

    private void ApplyAtTime(float time)
    {
        if (_profile == null || _cameraTransform == null || _orbitPivot == null)
            return;

        float introEnd = Mathf.Max(0f, _profile.IntroDuration);
        float zoomEnd = introEnd + Mathf.Max(0.01f, _profile.ImpactZoomDuration);
        float pullbackEnd = zoomEnd + Mathf.Max(0.01f, _profile.AfterPullbackDuration);
        float timelineEnd = GetTimelineEndTime();
        float showcaseDuration = Mathf.Max(0.01f, _profile.FinalShowcaseDuration);
        float showcaseStart = Mathf.Max(pullbackEnd + 0.01f, timelineEnd - showcaseDuration);
        float scanEnd = Mathf.Min(
            pullbackEnd + Mathf.Max(0.01f, _profile.AfterScanDuration),
            showcaseStart);
        if (scanEnd <= pullbackEnd)
            scanEnd = pullbackEnd + 0.01f;

        Vector3 cameraPosition;
        Vector3 lookLocalPosition;

        if (time < introEnd)
        {
            cameraPosition = _profile.BeforeIntroCameraLocalPosition;
            lookLocalPosition = GetLookPosition(_profile.BeforeLookHeight);
        }
        else if (time < zoomEnd)
        {
            float progress = Evaluate(_impactZoomCurve, Mathf.InverseLerp(introEnd, zoomEnd, time));
            cameraPosition = Vector3.Lerp(
                _profile.BeforeIntroCameraLocalPosition,
                _profile.BeforeImpactZoomCameraLocalPosition,
                progress);
            lookLocalPosition = GetLookPosition(
                Mathf.Lerp(_profile.BeforeLookHeight, _profile.AfterFootLookHeight, progress));
        }
        else if (time < pullbackEnd)
        {
            float progress = Evaluate(_smoothScanCurve, Mathf.InverseLerp(zoomEnd, pullbackEnd, time));
            cameraPosition = Vector3.Lerp(
                _profile.AfterCloseCameraLocalPosition,
                _profile.AfterPullbackCameraLocalPosition,
                progress);
            lookLocalPosition = GetLookPosition(_profile.AfterFootLookHeight);
        }
        else if (time < scanEnd)
        {
            float rawProgress = Mathf.InverseLerp(pullbackEnd, scanEnd, time);
            float progress = EvaluateScanProgress(rawProgress);
            cameraPosition = Vector3.Lerp(
                _profile.AfterPullbackCameraLocalPosition,
                _profile.AfterHeadCameraLocalPosition,
                progress);
            lookLocalPosition = GetLookPosition(
                Mathf.Lerp(_profile.AfterFootLookHeight, _profile.AfterHeadLookHeight, progress));
        }
        else if (time < showcaseStart)
        {
            cameraPosition = _profile.AfterHeadCameraLocalPosition;
            lookLocalPosition = GetLookPosition(_profile.AfterHeadLookHeight);
        }
        else if (time < timelineEnd)
        {
            float progress = Evaluate(_smoothScanCurve, Mathf.InverseLerp(showcaseStart, timelineEnd, time));
            cameraPosition = Vector3.Lerp(
                _profile.AfterHeadCameraLocalPosition,
                _profile.AfterFullShotCameraLocalPosition,
                progress);
            lookLocalPosition = Vector3.Lerp(
                GetLookPosition(_profile.AfterHeadLookHeight),
                GetFinalLookPosition(),
                progress);
        }
        else
        {
            cameraPosition = _profile.AfterFullShotCameraLocalPosition;
            lookLocalPosition = GetFinalLookPosition();
        }

        _cameraTransform.localPosition = cameraPosition;
        SetLookPosition(lookLocalPosition);
        LookAtPivot();
    }

    private void SetLookPosition(Vector3 lookLocalPosition)
    {
        _orbitPivot.localPosition = lookLocalPosition;
    }

    private static Vector3 GetLookPosition(float lookHeight)
    {
        return new Vector3(0f, lookHeight, 0f);
    }

    private Vector3 GetFinalLookPosition()
    {
        Vector3 lookPosition = _profile.AfterFullShotLookLocalPosition;
        if (lookPosition == Vector3.zero)
            lookPosition.y = _profile.AfterFullShotLookHeight;
        return lookPosition;
    }

    private void LookAtPivot()
    {
        Vector3 direction = _orbitPivot.position - _cameraTransform.position;
        if (direction.sqrMagnitude <= 0.0001f) return;

        _cameraTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private static float Evaluate(AnimationCurve curve, float value)
    {
        return curve != null ? curve.Evaluate(Mathf.Clamp01(value)) : Mathf.SmoothStep(0f, 1f, value);
    }

    private float EvaluateScanProgress(float value)
    {
        value = Mathf.Clamp01(value);
        float power = _profile != null ? Mathf.Max(0.1f, _profile.AfterScanVerticalCurvePower) : 1.25f;
        return Mathf.Pow(value, power);
    }

    private float GetTimelineEndTime()
    {
        if (_director != null &&
            _director.playableAsset != null &&
            !double.IsNaN(_director.duration) &&
            !double.IsInfinity(_director.duration) &&
            _director.duration > 0d)
        {
            return (float)_director.duration;
        }

        return _profile.IntroDuration
            + _profile.ImpactZoomDuration
            + _profile.AfterPullbackDuration
            + _profile.AfterScanDuration
            + _profile.FinalShowcaseDuration;
    }
}
