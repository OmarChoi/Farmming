using NUnit.Framework;

public class GameTimeTests
{
    [Test]
    public void AddMinutes_WrapsAcrossMidnight()
    {
        GameTime time = new GameTime(23, 50);

        GameTime advancedTime = time + 20;

        Assert.That(advancedTime.Hour, Is.EqualTo(0));
        Assert.That(advancedTime.Minute, Is.EqualTo(10));
    }

    [Test]
    public void NormalizeClock_ConvertsBoundaryToMidnight()
    {
        GameTime boundary = new GameTime(24, 0);

        GameTime normalizedTime = boundary.NormalizeClock();

        Assert.That(normalizedTime.Hour, Is.EqualTo(0));
        Assert.That(normalizedTime.Minute, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WrapsOutOfRangeInput()
    {
        GameTime wrappedTime = new GameTime(24, 30);

        Assert.That(wrappedTime.Hour, Is.EqualTo(0));
        Assert.That(wrappedTime.Minute, Is.EqualTo(30));
    }
}
