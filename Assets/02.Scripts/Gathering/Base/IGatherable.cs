public interface IGatherable
{
    public bool TryGather(GatheringInfo damage);
    public GatheringObjectSO GatheringData { get; }
}
