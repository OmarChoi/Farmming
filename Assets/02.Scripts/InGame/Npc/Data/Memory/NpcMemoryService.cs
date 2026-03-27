using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class NpcMemoryService
{
    private readonly INpcMemoryRepository _repository;
    private readonly INpcMemoryRagService _ragService;

    public NpcMemoryService(INpcMemoryRepository repository, INpcMemoryRagService ragService)
    {
        _repository = repository;
        _ragService = ragService;
    }

    public UniTask<NpcMemoryProfile> LoadProfileAsync(string npcId, string playerId)
    {
        return _repository.LoadMemoryAsync(npcId, playerId);
    }

    public UniTask<List<string>> SearchRelevantAsync(string npcId, string playerId, string query, int topK = 4)
    {
        return _ragService.SearchRelevantMemoriesAsync(npcId, playerId, query, topK);
    }

    public async UniTask SaveAfterDialogueAsync(
        NpcMemoryProfile profile,
        IReadOnlyList<DialogueTurnRecord> sessionTurns,
        string sessionSummary,
        List<NpcMemoryEntry> extractedMemories)
    {
        profile.RecentTurns.Clear();

        int start = Math.Max(0, sessionTurns.Count - 4);
        for (int i = start; i < sessionTurns.Count; i++)
        {
            profile.RecentTurns.Add(sessionTurns[i]);
        }

        profile.RollingSummary = sessionSummary ?? string.Empty;

        if (extractedMemories != null)
        {
            foreach (var memory in extractedMemories)
            {
                profile.Entries.Add(memory);
            }
        }

        await _repository.SaveMemoryAsync(profile);
        await _ragService.RebuildIndexMemoryRagAsync(profile);
    }
}
