using System;
using UnityEngine;

public class TestTimeManager : MonoBehaviour
{
    public static TestTimeManager Instance { get; private set; }

    [SerializeField] private int _currentTime = 800;
    [SerializeField] private float _timeInterval = 2f;
    [SerializeField] private int _timeStep = 50;

    public int CurrentTime => _currentTime;

    public event Action<int> OnTimeChanged;

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
        }

        Debug.Log($"Time : {_currentTime}");
        OnTimeChanged?.Invoke(_currentTime);
    }
}
