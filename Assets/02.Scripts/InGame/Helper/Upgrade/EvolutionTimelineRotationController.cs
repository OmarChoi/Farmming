using UnityEngine;
using UnityEngine.Playables;

public class EvolutionTimelineRotationController : MonoBehaviour
{
    [Header("Runtime References")]
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private Transform _beforeModelRoot;
    [SerializeField] private Transform _afterModelRoot;

    [Header("Curves")]
    [SerializeField] private AnimationCurve _accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve _decelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve _frontAlignCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private HelperEvolutionProfileSO _profile;
    private Vector3 _beforeBaseEuler;
    private Vector3 _afterBaseEuler;
    private bool _isPlaying;

    public void Configure(
        HelperEvolutionProfileSO profile,
        PlayableDirector director,
        Transform beforeModelRoot,
        Transform afterModelRoot,
        Vector3 beforeBaseEuler,
        Vector3 afterBaseEuler)
    {
        _profile = profile;
        _director = director;
        _beforeModelRoot = beforeModelRoot;
        _afterModelRoot = afterModelRoot;
        _beforeBaseEuler = beforeBaseEuler;
        _afterBaseEuler = afterBaseEuler;
    }

    public void Play()
    {
        _isPlaying = _profile != null && _beforeModelRoot != null && _afterModelRoot != null;
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
        float swapTime = Mathf.Max(0.01f, GetSwapTime());
        float timelineEnd = Mathf.Max(swapTime + 0.01f, GetTimelineEndTime());
        float maxSpeed = Mathf.Max(0f, _profile.TimelineSpinMaxSpeed);
        float accelerationDuration = Mathf.Max(0.01f, _profile.BeforeSpinAccelerationDuration);

        float angleAtSwap = GetBeforeAngle(swapTime, maxSpeed, accelerationDuration);
        float speedAtSwap = GetBeforeSpeed(swapTime, maxSpeed, accelerationDuration);

        if (time < swapTime)
        {
            float angle = GetBeforeAngle(time, maxSpeed, accelerationDuration);
            ApplyYaw(_beforeModelRoot, _beforeBaseEuler, angle);
            return;
        }

        float afterDuration = Mathf.Max(0.01f, timelineEnd - swapTime);
        float alignDuration = Mathf.Clamp(
            _profile.FinalRotationAlignDuration,
            0.01f,
            afterDuration);
        float alignStartTime = timelineEnd - alignDuration;
        float afterTime = Mathf.Clamp(time - swapTime, 0f, afterDuration);
        float afterAngle;

        if (time < alignStartTime)
        {
            afterAngle = angleAtSwap + GetAfterAngle(afterTime, afterDuration, speedAtSwap);
        }
        else
        {
            float alignStartAfterTime = Mathf.Clamp(alignStartTime - swapTime, 0f, afterDuration);
            float alignStartAngle = angleAtSwap + GetAfterAngle(alignStartAfterTime, afterDuration, speedAtSwap);
            float finalFrontAngle = GetFinalFrontAngle(alignStartAngle);
            float progress = Evaluate(_frontAlignCurve, Mathf.InverseLerp(alignStartTime, timelineEnd, time));
            afterAngle = Mathf.Lerp(alignStartAngle, finalFrontAngle, progress);
        }

        ApplyYaw(_afterModelRoot, _afterBaseEuler, afterAngle);
    }

    private float GetBeforeAngle(float time, float maxSpeed, float accelerationDuration)
    {
        time = Mathf.Max(0f, time);
        float acceleratedTime = Mathf.Min(time, accelerationDuration);
        float angle = IntegrateCurve(_accelerationCurve, acceleratedTime / accelerationDuration)
            * maxSpeed
            * accelerationDuration;

        if (time > accelerationDuration)
            angle += maxSpeed * (time - accelerationDuration);

        return angle;
    }

    private float GetBeforeSpeed(float time, float maxSpeed, float accelerationDuration)
    {
        if (time >= accelerationDuration)
            return maxSpeed;

        float progress = Mathf.Clamp01(time / accelerationDuration);
        return maxSpeed * Evaluate(_accelerationCurve, progress);
    }

    private float GetAfterAngle(float time, float duration, float startSpeed)
    {
        if (startSpeed <= 0f)
            return 0f;

        float progress = Mathf.Clamp01(time / duration);
        float decelArea = IntegrateCurve(_decelerationCurve, progress);
        return startSpeed * duration * (progress - decelArea);
    }

    private static void ApplyYaw(Transform target, Vector3 baseEuler, float yaw)
    {
        if (target == null) return;
        target.localEulerAngles = new Vector3(baseEuler.x, baseEuler.y + yaw, baseEuler.z);
    }

    private float GetFinalFrontAngle(float fromAngle)
    {
        float targetOffset = _profile != null ? _profile.FinalFrontYawOffset : 0f;
        float deltaToNextFront = Mathf.Repeat(targetOffset - fromAngle, 360f);
        float extraTurns = _profile != null ? Mathf.Max(0f, _profile.FinalRotationExtraTurns) : 0f;
        return fromAngle + deltaToNextFront + extraTurns * 360f;
    }

    private float GetSwapTime()
    {
        if (_profile != null && _profile.ModelSwapTime > 0f)
            return _profile.ModelSwapTime;

        return 4.65f;
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

        return 12f;
    }

    private static float Evaluate(AnimationCurve curve, float value)
    {
        return curve != null ? curve.Evaluate(Mathf.Clamp01(value)) : Mathf.SmoothStep(0f, 1f, value);
    }

    private static float IntegrateCurve(AnimationCurve curve, float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (progress <= 0f) return 0f;

        const int samples = 24;
        float step = progress / samples;
        float area = 0f;
        float previous = Evaluate(curve, 0f);

        for (int i = 1; i <= samples; i++)
        {
            float x = step * i;
            float current = Evaluate(curve, x);
            area += (previous + current) * 0.5f * step;
            previous = current;
        }

        return area;
    }
}
