using NUnit.Framework;
using UnityEngine;

public class TimeSystemTests
{
    private GameObject _gameObject;
    private TimeSystem _timeSystem;
    private TimeSettingSO _settings;

    [SetUp]
    public void SetUp()
    {
        _gameObject = new GameObject("TimeSystemTest");
        _timeSystem = _gameObject.AddComponent<TimeSystem>();

        _settings = ScriptableObject.CreateInstance<TimeSettingSO>();
        _settings.DefaultDay = 1;
        _settings.DefaultTime = new GameTime(5, 59);
        _settings.DayStartTime = new GameTime(6, 0);
        _settings.DayEndTime = new GameTime(18, 0);
        _settings.GameMinutesPerSecond = 1f;

        _timeSystem.Initialize(_settings);
    }

    [TearDown]
    public void TearDown()
    {
        if (_settings != null)
        {
            Object.DestroyImmediate(_settings);
        }

        if (_gameObject != null)
        {
            Object.DestroyImmediate(_gameObject);
        }
    }

    [Test]
    public void Tick_DoesNotAdvanceBeforeOneMinuteElapsed()
    {
        _timeSystem.Tick(0.5f);

        Assert.That(_timeSystem.CurrentTime, Is.EqualTo(new GameTime(5, 59)));
    }

    [Test]
    public void Tick_EmitsMinuteAndDayStartEvents()
    {
        int minuteChangedCount = 0;
        int dayStartedCount = 0;

        _timeSystem.OnMinuteChanged += _ => minuteChangedCount++;
        _timeSystem.OnDayStarted += () => dayStartedCount++;

        _timeSystem.Tick(1f);

        Assert.That(_timeSystem.CurrentTime, Is.EqualTo(new GameTime(6, 0)));
        Assert.That(minuteChangedCount, Is.EqualTo(1));
        Assert.That(dayStartedCount, Is.EqualTo(1));
    }

    [Test]
    public void Tick_UsesGameMinutesPerSecondRate()
    {
        _settings.DefaultTime = new GameTime(5, 59);
        _settings.GameMinutesPerSecond = 2f;
        _timeSystem.Initialize(_settings);

        _timeSystem.Tick(0.5f);

        Assert.That(_timeSystem.CurrentTime, Is.EqualTo(new GameTime(6, 0)));
    }

    [Test]
    public void Tick_EmitsDayChangedAndDayEndedEventsAtMidnight()
    {
        _settings.DefaultTime = new GameTime(23, 59);
        _settings.DayEndTime = new GameTime(24, 0);
        _timeSystem.Initialize(_settings);

        int dayChangedCount = 0;
        int dayEndedCount = 0;

        _timeSystem.OnDayChanged += _ => dayChangedCount++;
        _timeSystem.OnDayEnded += () => dayEndedCount++;

        _timeSystem.Tick(1f);

        Assert.That(_timeSystem.CurrentDay, Is.EqualTo(2));
        Assert.That(_timeSystem.CurrentTime, Is.EqualTo(new GameTime(0, 0)));
        Assert.That(dayChangedCount, Is.EqualTo(1));
        Assert.That(dayEndedCount, Is.EqualTo(1));
    }
}
