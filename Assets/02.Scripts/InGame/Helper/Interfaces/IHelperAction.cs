public interface IHelperAction
{
    void InteractPrimary(TerrainCell cell);
    void InteractSecondary(TerrainCell cell);
    float GetSecondaryCost() => -1f;
    bool CanInteractPrimary(TerrainCell cell) => cell != null;
    bool CanInteractSecondary(TerrainCell cell) => cell != null;
}

public interface ISecondaryInteractBlockNotifier
{
    void NotifySecondaryInteractBlocked(TerrainCell cell);
}
