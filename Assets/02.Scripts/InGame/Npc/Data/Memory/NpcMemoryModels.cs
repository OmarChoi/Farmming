using System;
using System.Collections.Generic;

[Serializable]
public class NpcMemoryEntry
{
    public string MemoryId;
    public string NpcId;
    public string PlayerId;
    public string Category; // Fact / Impression / Episode / Promise / Preference
    public string Text;
    public long CreatedAtTicks;
    public long UpdatedAtTicks;
    public int Priority; // 0~100
}

[Serializable]
public class NpcMemoryProfile
{
    public string NpcId;
    public string PlayerId;

    public int Friendship;
    public string FriendshipStep;

    public List<NpcMemoryEntry> Entries = new();
    public List<DialogueTurnRecord> RecentTurns = new(); // 최근 2~4턴 복원용
    public string RollingSummary = string.Empty;
}

[Serializable]
public class DialogueTurnRecord
{
    public string Role; // user / assistant
    public string Text;
    public long Ticks;
}
