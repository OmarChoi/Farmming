using UnityEngine;
using UnityEngine.UI;

public class UI_Time : MonoBehaviour
{
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private RectTransform _timeImage;
    [SerializeField] private Sprite _nightBackground;
    [SerializeField] private Sprite _dayBackground;

    private void Start()
    {
        if (TimeEvents.IsDayTime)
        {
            SetMorningImage();
        }
        else
        {
            SetNightImage();
        }

        UpdateTime(TimeEvents.CurrentTime);
    }

    private void OnEnable()
    {
        TimeEvents.OnMinuteChanged += UpdateTime;
        TimeEvents.OnNetSunRise += SetMorningImage;
        TimeEvents.OnNetSunSet += SetNightImage;
    }

    private void OnDisable()
    {
        TimeEvents.OnMinuteChanged -= UpdateTime;
        TimeEvents.OnNetSunRise -= SetMorningImage;
        TimeEvents.OnNetSunSet -= SetNightImage;
    }

    private void SetMorningImage()
    {
        _backgroundImage.sprite = _dayBackground;
    }

    private void SetNightImage()
    {
        _backgroundImage.sprite = _nightBackground;
    }

    // 08:00을 0도 기준으로 삼음 (한 시간 = 15도)
    private const int AngleOriginMinutes = 8 * GameTime.MinutesPerHour;

    private void UpdateTime(GameTime time)
    {
        float angle = ((time.TotalMinutes - AngleOriginMinutes) / (float)GameTime.MinutesPerDay) * 360f;
        _timeImage.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }
}