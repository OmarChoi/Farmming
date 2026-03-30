using UnityEngine;

public class TestDaySkip : MonoBehaviour
{
    [SerializeField] private TimeSystem _timeSystem;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            _timeSystem.SkipToNextDay();
        }
    }
}
