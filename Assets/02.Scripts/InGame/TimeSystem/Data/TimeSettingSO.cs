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
    [Tooltip("How many game minutes pass per real second.")]
    [Min(0f)]
    public float GameMinutesPerSecond = 1f;

    [SerializeField, HideInInspector, FormerlySerializedAs("RealSecondsPerGameMinute")]
    private float _legacyRealSecondsPerGameMinute = -1f;

    private void OnValidate()
    {
        if (_legacyRealSecondsPerGameMinute > 0f)
        {
            GameMinutesPerSecond = 1f / _legacyRealSecondsPerGameMinute;
            _legacyRealSecondsPerGameMinute = -1f;
        }

        DefaultDay = Mathf.Max(1, DefaultDay);
        GameMinutesPerSecond = Mathf.Max(0f, GameMinutesPerSecond);

        DefaultTime = DefaultTime.NormalizeClock();
        DayStartTime = DayStartTime.NormalizeBoundary();
        DayEndTime = DayEndTime.NormalizeBoundary();
        WakeUpTime = WakeUpTime.NormalizeClock();
        SunriseTime = SunriseTime.NormalizeClock();
        SunsetTime = SunsetTime.NormalizeClock();
    }
}
