using TMPro;
using UnityEngine;

public class UI_Time : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timeText;

    private void Start()
    {
        UpdateTime(TimeEvents.CurrentTime);
    }

    private void OnEnable()
    {
        TimeEvents.OnMinuteChanged += UpdateTime;
    }

    private void OnDisable()
    {
        TimeEvents.OnMinuteChanged -= UpdateTime;
    }

    private void UpdateTime(GameTime time)
    {
        _timeText.text = time.To12HourString();
    }
}
