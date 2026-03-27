using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LLMUnity;

public class NpcMemoryLlmRagService : MonoBehaviour, INpcMemoryRagService
{
    [SerializeField] private LLM _embeddingLlm;
    private RAG _rag;

    // 추후 시간 되면 npcId_playerId 단위 문서 캐시로 확장하는 것을 고려하겠습니다.
    private readonly Dictionary<string, List<NpcMemoryRagDocument>> _docsByKey = new();

    public async UniTask InitializeMemoryRagAsync()
    {
        _rag = new RAG();
        _rag.Init(SearchMethods.DBSearch, ChunkingMethods.NoChunking, _embeddingLlm);
        _rag.ReturnChunks(true);

        await UniTask.Yield();
    }

    public async UniTask RebuildIndexMemoryRagAsync(NpcMemoryProfile profile)
    {
        string key = MakeKey(profile.NpcId, profile.PlayerId);

        var docs = new List<NpcMemoryRagDocument>();
        foreach (var entry in profile.Entries)
        {
            docs.Add(new NpcMemoryRagDocument
            {
                MemoryId = entry.MemoryId,
                NpcId = entry.NpcId,
                PlayerId = entry.PlayerId,
                Category = entry.Category,
                Content = $"[{entry.Category}] {entry.Text}"
            });
        }

        _docsByKey[key] = docs;
        await UniTask.Yield();
    }

    public async UniTask<List<string>> SearchRelevantMemoriesAsync(string npcId, string playerId, string query, int topK = 4)
    {
        string key = MakeKey(npcId, playerId);

        if (!_docsByKey.TryGetValue(key, out var docs) || docs.Count == 0)
        {
            return new List<string>();
        }

        var result = new List<string>();

        foreach (var doc in docs)
        {
            if (result.Count >= topK) break;

            if (query.Contains("좋아") && doc.Category == "Preference")
            {
                result.Add(doc.Content);
                continue;
            }

            if (query.Contains("전에") || query.Contains("기억"))
            {
                result.Add(doc.Content);
            }
        }

        await UniTask.Yield();
        return result;
    }

    private string MakeKey(string npcId, string playerId) => $"{npcId}::{playerId}";
}
