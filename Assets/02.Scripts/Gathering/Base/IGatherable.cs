public interface IGatherable
{
    public bool TryGather(int damage);
    public GatheringObjectSO GatheringData { get; }
}
