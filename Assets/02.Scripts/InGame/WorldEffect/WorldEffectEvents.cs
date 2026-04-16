using System;

public static class WorldEffectEvents
{
    public static event Action<WorldEffectEntry> OnEffectAdded;
    public static event Action<WorldEffectEntry> OnEffectRemoved;
    public static event Action OnEffectsChanged;

    internal static void InvokeEffectAdded(WorldEffectEntry entry) => OnEffectAdded?.Invoke(entry);
    internal static void InvokeEffectRemoved(WorldEffectEntry entry) => OnEffectRemoved?.Invoke(entry);
    internal static void InvokeEffectsChanged() => OnEffectsChanged?.Invoke();
}
