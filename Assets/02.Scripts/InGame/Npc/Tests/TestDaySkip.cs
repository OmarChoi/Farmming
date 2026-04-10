using UnityEngine;

public class TestDaySkip : MonoBehaviour
{
    public TimeSystem TimeSystem;

    private void SetTimeSystem()
    {
        TimeSystem = FindFirstObjectByType<TimeSystem>();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (TimeSystem == null)
            {
                SetTimeSystem();
            }
            TimeSystem.SkipToNextDay();
        }
    }
}
