public interface IWorldEffect
{
    string EffectId { get; }
    EWorldEffectKind Kind { get; }

    void Apply(WorldEffectEntry entry);
    void Remove(WorldEffectEntry entry);
}

public interface IWorldBuff : IWorldEffect
{
}

public interface IWorldDebuff : IWorldEffect
{
}
