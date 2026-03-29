using TMPro;
using UnityEngine;

public class UI_Time : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timeText;

    private void Start()
    {
        UpdateTime(TimeEvents.CurrentTime);
        TimeEvents.OnMinuteChanged += UpdateTime;
    }

    private void OnDestroy()
    {
        TimeEvents.OnMinuteChanged -= UpdateTime;
    }

    private void UpdateTime(GameTime time)
    {
        _timeText.text = time.To12HourString();
    }
}
