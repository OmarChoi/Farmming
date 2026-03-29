using System;

[Serializable]
public class NpcMemoryCategoryDefinition
{
    public EMemoryCategory Category;
    public EMemoryUpdatePolicy Policy;
    public bool IsUnique;
    public int MaxCount;
}
