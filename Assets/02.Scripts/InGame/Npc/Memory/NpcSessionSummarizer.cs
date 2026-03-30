using System.Collections.Generic;
using System.Text;

public class NpcSessionSummarizer
{
    public string Summarize(IReadOnlyList<DialogueTurnRecord> turns)
    {
        if (turns == null || turns.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("최근 대화 요약: ");

        int count = 0;
        for (int i = 0; i < turns.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(turns[i].Text)) continue;

            sb.Append($"[{turns[i].Role}] {turns[i].Text} ");
            count++;
            if (count >= 3) break;
        }

        return sb.ToString().Trim();
    }
}
