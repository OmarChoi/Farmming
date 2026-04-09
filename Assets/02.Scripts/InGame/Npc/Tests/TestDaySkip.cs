using UnityEngine;

public class TestDaySkip : MonoBehaviour
{
    private TimeSystem _timeSystem;

    private void Start()
    {
        _timeSystem = FindFirstObjectByType<TimeSystem>();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            _timeSystem.SkipToNextDay();
        }
    }
}
