using System;
using System.Collections.Generic;

public class NpcMemoryExtractor
{
    public List<NpcMemoryEntry> Extract(string npcId, string playerId, IReadOnlyList<DialogueTurnRecord> turns)
    {
        var result = new List<NpcMemoryEntry>();

        foreach (var turn in turns)
        {
            if (turn.Role != "user") continue;
            if (string.IsNullOrWhiteSpace(turn.Text)) continue;

            string text = turn.Text.Trim();

            if (TryExtractPlayerName(text, out string playerName))
            {
                result.Add(Create(
                    npcId,
                    playerId,
                    EMemoryCategory.PlayerName,
                    $"플레이어의 이름은 {playerName}이다.",
                    100));
            }

            if (text.Contains("좋아해"))
            {
                result.Add(Create(
                    npcId,
                    playerId,
                    EMemoryCategory.PlayerPreference,
                    $"플레이어가 '{text}' 라고 말했다.",
                    60));
            }
            else if (text.Contains("싫어해"))
            {
                result.Add(Create(
                    npcId,
                    playerId,
                    EMemoryCategory.PlayerPreference,
                    $"플레이어가 '{text}' 라고 말했다.",
                    60));
            }
            else if (text.Contains("기억해") || text.Contains("약속"))
            {
                result.Add(Create(
                    npcId,
                    playerId,
                    EMemoryCategory.Promise,
                    $"플레이어와 관련된 약속/기억 단서: {text}",
                    80));
            }

            result.Add(Create(
                npcId,
                playerId,
                EMemoryCategory.Episode,
                $"플레이어가 '{text}' 라고 말했다.",
                20));
        }

        return result;
    }

    private bool TryExtractPlayerName(string text, out string playerName)
    {
        playerName = null;

        if (text.StartsWith("나는 ") && text.EndsWith("야"))
        {
            playerName = text.Replace("나는 ", "").Replace("야", "").Trim();
            return !string.IsNullOrEmpty(playerName);
        }

        if (text.StartsWith("난 ") && text.EndsWith("야"))
        {
            playerName = text.Replace("난 ", "").Replace("야", "").Trim();
            return !string.IsNullOrEmpty(playerName);
        }

        if (text.StartsWith("내 이름은 ") && text.EndsWith("야"))
        {
            playerName = text.Replace("내 이름은 ", "").Replace("야", "").Trim();
            return !string.IsNullOrEmpty(playerName);
        }

        return false;
    }

    private NpcMemoryEntry Create(string npcId, string playerId, EMemoryCategory category, string text, int priority)
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
