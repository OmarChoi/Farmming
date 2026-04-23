using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "TimeSettings", menuName = "TimeSettings")]
public class TimeSettingSO : ScriptableObject
{
    [Header("Init Game Time")]
    public int DefaultDay = 1;
    public GameTime DefaultTime = new GameTime(8, 0);

    [Space(10)]
    [Header("Day Start and End")]
    public GameTime DayStartTime = new GameTime(6, 0);
    public GameTime DayEndTime = new GameTime(24, 0);
    public GameTime WakeUpTime = new GameTime(6, 0);

    [Space(10)]
    [Header("Sunrise and Sunset Times")]
    public GameTime SunriseTime = new GameTime(8, 0);
    public GameTime SunsetTime = new GameTime(20, 0);

    [Space(10)]
    [Header("Real Time to Game Time Ratio")]
    [Tooltip("How many game minutes pass per real second during daytime.")]
    [Min(0f)]
    public float DayGameMinutesPerSecond = 1f;

    [Tooltip("How many game minutes pass per real second during nighttime.")]
    [Min(0f)]
    public float NightGameMinutesPerSecond = 1f;

    [SerializeField, HideInInspector, FormerlySerializedAs("RealSecondsPerGameMinute")]
    private float _legacyRealSecondsPerGameMinute = -1f;

    [SerializeField, HideInInspector, FormerlySerializedAs("GameMinutesPerSecond")]
    private float _legacyUnifiedGameMinutesPerSecond = -1f;

    private void OnValidate()
    {
        if (_legacyRealSecondsPerGameMinute > 0f)
        {
            float converted = 1f / _legacyRealSecondsPerGameMinute;
            DayGameMinutesPerSecond = converted;
            NightGameMinutesPerSecond = converted;
            _legacyRealSecondsPerGameMinute = -1f;
        }

        if (_legacyUnifiedGameMinutesPerSecond > 0f)
        {
            if (DayGameMinutesPerSecond <= 0f)
                DayGameMinutesPerSecond = _legacyUnifiedGameMinutesPerSecond;
            if (NightGameMinutesPerSecond <= 0f)
                NightGameMinutesPerSecond = _legacyUnifiedGameMinutesPerSecond;

            _legacyUnifiedGameMinutesPerSecond = -1f;
        }

        DefaultDay = Mathf.Max(1, DefaultDay);
        DayGameMinutesPerSecond = Mathf.Max(0f, DayGameMinutesPerSecond);
        NightGameMinutesPerSecond = Mathf.Max(0f, NightGameMinutesPerSecond);

        DefaultTime = DefaultTime.NormalizeClock();
        DayStartTime = DayStartTime.NormalizeBoundary();
        DayEndTime = DayEndTime.NormalizeBoundary();
        WakeUpTime = WakeUpTime.NormalizeClock();
        SunriseTime = SunriseTime.NormalizeClock();
        SunsetTime = SunsetTime.NormalizeClock();
    }
}
