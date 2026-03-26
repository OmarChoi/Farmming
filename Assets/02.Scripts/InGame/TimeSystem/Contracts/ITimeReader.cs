public interface ITimeReader
{
    public int CurrentDay { get; }
    public GameTime CurrentTime { get; }
    public bool IsDayTime { get; }
}
