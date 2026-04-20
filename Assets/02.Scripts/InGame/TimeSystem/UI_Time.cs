using UnityEngine;
using UnityEngine.UI;

public class UI_Time : MonoBehaviour
{
    private const int AngleOriginMinutes = 8 * GameTime.MinutesPerHour;

    private const int MaxDegree = 85;
    [SerializeField] private Image _arrowImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Sprite[] _arrowSprites;
    [SerializeField] private Sprite[] _iconSprites;

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

    private void Start()
    {
        UpdateTime(TimeEvents.CurrentTime);
        if (TimeEvents.IsDayTime) SetMorningImage();
        else SetNightImage();
    }
    
    private void SetMorningImage()
    {
        _arrowImage.sprite = _arrowSprites[0];
        _iconImage.sprite = _iconSprites[0];
    }

    private void SetNightImage()
    {
        _arrowImage.sprite = _arrowSprites[1];
        _iconImage.sprite = _iconSprites[1];
    }
    
    private void UpdateTime(GameTime time)
    {
        float progress = (time.TotalMinutes - AngleOriginMinutes) / (float)GameTime.MinutesPerDay;
        float angle = Mathf.Repeat(progress * MaxDegree * 2, MaxDegree * 2) - MaxDegree;
        _arrowImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }
}