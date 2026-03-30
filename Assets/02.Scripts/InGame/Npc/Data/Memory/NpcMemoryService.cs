using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NpcMemoryService
{
    private readonly INpcMemoryRepository _repository;
    private readonly INpcMemoryRagService _ragService;

    private const int MaxRecentTurnsToRestore = 4;

    public NpcMemoryService(INpcMemoryRepository repository, INpcMemoryRagService ragService)
    {
        _repository = repository;
        _ragService = ragService;
    }

    public UniTask<NpcMemoryProfile> LoadProfileAsync(string npcId, string playerId)
    {
        return _repository.LoadMemoryAsync(npcId, playerId);
    }

    public UniTask<List<string>> SearchRelevantAsync(string npcId, string playerId, string query, int topK = MaxRecentTurnsToRestore)
    {
        return _ragService.SearchRelevantMemoriesAsync(npcId, playerId, query, topK);
    }

    public async UniTask SaveAfterDialogueAsync(NpcMemoryProfile profile, IReadOnlyList<DialogueTurnRecord> sessionTurns, string sessionSummary)
    {
        if (profile == null) return;

        profile.RecentTurns.Clear();

        int start = Math.Max(0, sessionTurns.Count - MaxRecentTurnsToRestore);
        for (int i = start; i < sessionTurns.Count; i++)
        {
            profile.RecentTurns.Add(sessionTurns[i]);
        }

        profile.RollingSummary = sessionSummary ?? string.Empty;

        await _repository.SaveMemoryAsync(profile);

        if (_ragService != null)
        {
            await _ragService.RebuildIndexMemoryRagAsync(profile);
        }
    }
}
