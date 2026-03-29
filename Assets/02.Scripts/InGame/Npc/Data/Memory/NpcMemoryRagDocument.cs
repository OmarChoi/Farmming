using System;

[Serializable]
public class NpcMemoryRagDocument
{
    public string MemoryId;
    public string NpcId;
    public string PlayerId;
    public EMemoryCategory Category;
    public string Content;

    public int Priority;
    public long UpdatedAtTicks;

    public float[] Embedding;
}
