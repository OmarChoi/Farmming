using NUnit.Framework;

public class GameClockTests
{
    [Test]
    public void AdvanceMinutes_StartsDayAtConfiguredTime()
    {
        GameClock clock = new GameClock
        (
            1,
            new GameTime(5, 59),
            new GameTime(6, 0),
            new GameTime(24, 0)
        );

        clock.AdvanceMinutes(1);

        Assert.That(clock.CurrentDay, Is.EqualTo(1));
        Assert.That(clock.CurrentTime, Is.EqualTo(new GameTime(6, 0)));
        Assert.That(clock.IsDayTime, Is.True);
    }

    [Test]
    public void AdvanceMinutes_EndsDayAtConfiguredTime()
    {
        GameClock clock = new GameClock
        (
            1,
            new GameTime(17, 59),
            new GameTime(6, 0),
            new GameTime(18, 0)
        );

        clock.AdvanceMinutes(1);

        Assert.That(clock.CurrentTime, Is.EqualTo(new GameTime(18, 0)));
        Assert.That(clock.IsDayTime, Is.False);
    }

    [Test]
    public void AdvanceMinutes_IncreasesDayAfterMidnight()
    {
        GameClock clock = new GameClock
        (
            1,
            new GameTime(23, 59),
            new GameTime(6, 0),
            new GameTime(24, 0)
        );

        clock.AdvanceMinutes(1);

        Assert.That(clock.CurrentDay, Is.EqualTo(2));
        Assert.That(clock.CurrentTime, Is.EqualTo(new GameTime(0, 0)));
        Assert.That(clock.IsDayTime, Is.False);
    }
}