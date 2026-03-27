using System;
using UnityEngine;

public class TestTimeManager : MonoBehaviour
{
    public static TestTimeManager Instance { get; private set; }

    [SerializeField] private int _currentDay = 1;
    [SerializeField] private int _currentTime = 810;
    [SerializeField] private float _timeInterval = 2f;
    [SerializeField] private int _timeStep = 100;

    public int CurrentDay => _currentDay;
    public int CurrentTime => _currentTime;

    public event Action<int> OnTimeChanged;
    public event Action<int> OnDayChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InvokeRepeating(nameof(UpdateTime), 1f, _timeInterval);
    }

    private void UpdateTime()
    {
        _currentTime += _timeStep;

        if (_currentTime >= 2400)
        {
            _currentTime = 0;
            _currentDay++;

#if UNITY_EDITOR
            Debug.Log($"Day Changed : {_currentDay}");
#endif
            OnDayChanged?.Invoke(_currentDay);
        }

#if UNITY_EDITOR
        //Debug.Log($"Day : {_currentDay}, Time : {_currentTime}");
#endif

        OnTimeChanged?.Invoke(_currentTime);
    }
}
