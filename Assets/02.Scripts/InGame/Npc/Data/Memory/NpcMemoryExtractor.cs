using System;
using System.Collections.Generic;

public class NpcMemoryExtractor
{
    public List<NpcMemoryEntry> Extract(
        string npcId,
        string playerId,
        IReadOnlyList<DialogueTurnRecord> turns)
    {
        var result = new List<NpcMemoryEntry>();

        foreach (var turn in turns)
        {
            if (turn.Role != "user") continue;
            if (string.IsNullOrWhiteSpace(turn.Text)) continue;

            if (turn.Text.Contains("좋아해"))
            {
                result.Add(Create(npcId, playerId, "Preference", $"플레이어가 '{turn.Text}' 라고 말했다.", 60));
            }
            else if (turn.Text.Contains("싫어해"))
            {
                result.Add(Create(npcId, playerId, "Preference", $"플레이어가 '{turn.Text}' 라고 말했다.", 60));
            }
            else if (turn.Text.Contains("기억해") || turn.Text.Contains("약속"))
            {
                result.Add(Create(npcId, playerId, "Promise", $"플레이어와 관련된 약속/기억 단서: {turn.Text}", 80));
            }
        }

        return result;
    }

    private NpcMemoryEntry Create(string npcId, string playerId, string category, string text, int priority)
    {
        long now = DateTime.UtcNow.Ticks;

        return new NpcMemoryEntry
        {
            MemoryId = Guid.NewGuid().ToString("N"),
            NpcId = npcId,
            PlayerId = playerId,
            Category = category,
            Text = text,
            CreatedAtTicks = now,
            UpdatedAtTicks = now,
            Priority = priority
        };
    }
}
