using UnityEngine;

public class DinoRaceRunner : MonoBehaviour
{
    [SerializeField] private string _displayName = "Dino";
    [SerializeField] private Animator _animator;
    [SerializeField] private string _animationParam = "animation";
    [SerializeField] private int _idleAnimationValue = 1;
    [SerializeField] private int _stunAnimationValue = 9;
    [SerializeField] private int _slowDownAnimationValue = 15;
    [SerializeField] private int _speedUpAnimationValue = 18;
    [SerializeField] private int _runAnimationValue = 21;

    private Vector3 _startLocalPosition;
    private Quaternion _startLocalRotation;
    private Vector3 _trackForwardLocal = Vector3.forward;

    private DinoRaceEventPolicy _eventPolicy;
    private float _baseSpeed;
    private float _trackLength;
    private float _currentSpeed;
    private float _currentDistance;
    private float _nextEventTime;
    private float _eventEndTime;
    private EDinoRaceEventType _currentEventType;
    private bool _isEventActive;
    private bool _isFinished;
    private bool _hasStartedRunning;
    private int _finishRank;
    private int _laneIndex;

    public string DisplayName => string.IsNullOrEmpty(_displayName) ? gameObject.name : _displayName;
    public int LaneIndex => _laneIndex;
    public float CurrentSpeed => _currentSpeed;
    public float CurrentDistance => _currentDistance;
    public bool IsFinished => _isFinished;
    public int FinishRank => _finishRank;
    public EDinoRaceEventType CurrentEventType => _currentEventType;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _startLocalPosition = transform.localPosition;
        _startLocalRotation = transform.localRotation;
    }

    public void Configure(
        int laneIndex,
        float baseSpeed,
        float trackLength,
        Vector3 trackForwardLocal,
        DinoRaceEventPolicy eventPolicy)
    {
        _laneIndex = laneIndex;
        _baseSpeed = Mathf.Max(0f, baseSpeed);
        _trackLength = Mathf.Max(0.1f, trackLength);
        _trackForwardLocal = trackForwardLocal.sqrMagnitude < 0.01f
            ? Vector3.forward
            : trackForwardLocal.normalized;
        _eventPolicy = eventPolicy;
    }

    public void ResetForRace(float now)
    {
        _currentSpeed = _baseSpeed;
        _currentDistance = 0f;
        _nextEventTime = now + (_eventPolicy?.GetNextEventDelay() ?? float.MaxValue);
        _eventEndTime = 0f;
        _currentEventType = EDinoRaceEventType.None;
        _isEventActive = false;
        _isFinished = false;
        _hasStartedRunning = false;
        _finishRank = 0;

        transform.localPosition = _startLocalPosition;
        transform.localRotation = _startLocalRotation;
        UpdateAnimation();
    }

    public void StartRunning()
    {
        _hasStartedRunning = true;
        UpdateAnimation();
    }

    public void Tick(float now, float deltaTime)
    {
        if (_isFinished) return;

        if (_isEventActive && now >= _eventEndTime)
            EndEvent(now);

        if (!_isEventActive && now >= _nextEventTime)
            BeginEvent(now);

        _currentDistance += _currentSpeed * deltaTime;
        _currentDistance = Mathf.Max(0f, _currentDistance);

        ApplyPosition();
        UpdateAnimation();
    }

    public void Finish(int finishRank)
    {
        _isFinished = true;
        _finishRank = finishRank;
        _isEventActive = false;
        _currentEventType = EDinoRaceEventType.None;
        _currentSpeed = 0f;
        _currentDistance = _trackLength;

        ApplyPosition();
        UpdateAnimation();
    }

    public DinoRaceRunnerSnapshot CreateSnapshot()
    {
        return new DinoRaceRunnerSnapshot(
            _laneIndex,
            DisplayName,
            _currentDistance,
            _currentSpeed,
            _isFinished,
            _finishRank,
            _currentEventType);
    }

    private void BeginEvent(float now)
    {
        if (_eventPolicy == null)
        {
            _nextEventTime = float.MaxValue;
            return;
        }

        _currentEventType = _eventPolicy.RollEventType();
        if (_currentEventType == EDinoRaceEventType.None)
        {
            _nextEventTime = now + _eventPolicy.GetNextEventDelay();
            return;
        }

        _isEventActive = true;
        _currentSpeed = _eventPolicy.GetSpeedForEvent(_currentEventType, _baseSpeed);
        _eventEndTime = now + _eventPolicy.GetEventDuration(_currentEventType);
        UpdateAnimation();
    }

    private void EndEvent(float now)
    {
        _isEventActive = false;
        _currentEventType = EDinoRaceEventType.None;
        _currentSpeed = _baseSpeed;
        _eventEndTime = 0f;
        _nextEventTime = now + (_eventPolicy?.GetNextEventDelay() ?? float.MaxValue);
        UpdateAnimation();
    }

    private void ApplyPosition()
    {
        transform.localPosition = _startLocalPosition + _trackForwardLocal * _currentDistance;
    }

    private void UpdateAnimation()
    {
        if (_animator == null || string.IsNullOrEmpty(_animationParam)) return;

        int animationValue = GetAnimationValue();
        _animator.SetInteger(_animationParam, animationValue);
    }

    private int GetAnimationValue()
    {
        if (_isFinished || !_hasStartedRunning)
            return _idleAnimationValue;

        return _currentEventType switch
        {
            EDinoRaceEventType.Stun => _stunAnimationValue,
            EDinoRaceEventType.SlowDown => _slowDownAnimationValue,
            EDinoRaceEventType.SpeedUp => _speedUpAnimationValue,
            _ => _runAnimationValue
        };
    }
}
