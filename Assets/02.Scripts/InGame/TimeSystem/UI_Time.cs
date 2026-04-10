using UnityEngine;
using UnityEngine.UI;

public class UI_Time : MonoBehaviour
{
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private RectTransform _timeImage;
    [SerializeField] private Sprite _nightBackground;
    [SerializeField] private Sprite _dayBackground;
    [SerializeField] private Sprite _moonIcon;
    [SerializeField] private Sprite _sunIcon;
    [SerializeField] private float _orbitRadius = 100f;

    private Image _timeIconImage;

    private void Awake()
    {
        _timeIconImage = _timeImage.GetComponent<Image>();
    }

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
        _timeIconImage.sprite = _sunIcon;
        SetOrbitPosition(0f);
    }

    private void SetNightImage()
    {
        _backgroundImage.sprite = _nightBackground;
        _timeIconImage.sprite = _moonIcon;
        SetOrbitPosition(0f);
    }

    private void SetOrbitPosition(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        // angle 0  : 왼쪽 (-radius, 0)
        // angle 90 : 위쪽 (0, radius)
        // angle 180: 오른쪽 (radius, 0)
        float x = -Mathf.Cos(rad) * _orbitRadius;
        float y = Mathf.Sin(rad) * _orbitRadius;
        _timeImage.localPosition = new Vector3(x, y, _timeImage.localPosition.z);
    }

    private void UpdateTime(GameTime time)
    {
        TimeSettingSO settings = TimeEvents.Settings;
        if (settings == null) return;

        int sunriseMinutes = settings.SunriseTime.TotalMinutes;
        int sunsetMinutes = settings.SunsetTime.TotalMinutes;

        int elapsed;
        int total;
        if (TimeEvents.IsDayTime)
        {
            // 낮 : SunriseTime ~ SunsetTime 사이를 0 ~ 180도로 매핑
            elapsed = time.TotalMinutes - sunriseMinutes;
            total = sunsetMinutes - sunriseMinutes;
        }
        else
        {
            // 밤 : SunsetTime ~ SunriseTime(다음 날) 사이를 0 ~ 180도로 매핑
            elapsed = GameTime.WrapTotalMinutes(time.TotalMinutes - sunsetMinutes);
            total = GameTime.WrapTotalMinutes(sunriseMinutes - sunsetMinutes);
        }

        if (total <= 0) return;

        float angle = Mathf.Clamp(elapsed / (float)total, 0f, 1f) * 180f;
        SetOrbitPosition(angle);
    }
}
