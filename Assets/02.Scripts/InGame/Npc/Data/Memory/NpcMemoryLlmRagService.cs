using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LLMUnity;

public class NpcMemoryLlmRagService : MonoBehaviour, INpcMemoryRagService
{
    [SerializeField] private LLM _embeddingLlm;
    [SerializeField] private RAG _rag;

    private readonly Dictionary<string, List<NpcMemoryRagDocument>> _docsByKey = new();
    private bool _isInitialized;

    private void Awake()
    {
        if (_embeddingLlm == null)
        {
            _embeddingLlm = GetComponent<LLM>();
        }

        if (_rag == null)
        {
            _rag = GetComponent<RAG>();
        }
    }

    public async UniTask InitializeMemoryRagAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_embeddingLlm == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[NpcMemoryLlmRagService] _embeddingLlm 이 연결되지 않았습니다.");
#endif
            return;
        }

        if (_rag == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[NpcMemoryLlmRagService] RAG 컴포넌트를 찾지 못했습니다.");
#endif
            return;
        }

        _rag.Init(SearchMethods.DBSearch, ChunkingMethods.NoChunking, _embeddingLlm);
        _rag.ReturnChunks(true);

        _isInitialized = true;
        await UniTask.Yield();
    }

    public async UniTask RebuildIndexMemoryRagAsync(NpcMemoryProfile profile)
    {
        if (!_isInitialized)
        {
            await InitializeMemoryRagAsync();
        }

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
        if (!_isInitialized)
        {
            await InitializeMemoryRagAsync();
        }

        string key = MakeKey(npcId, playerId);

        if (!_docsByKey.TryGetValue(key, out var docs) || docs.Count == 0)
        {
            return new List<string>();
        }

        var result = new List<string>();

        foreach (var doc in docs)
        {
            if (result.Count >= topK) break;

            if (query.Contains("좋아") && doc.Category == EMemoryCategory.PlayerPreference)
            {
                result.Add(doc.Content);
                continue;
            }

            if (query.Contains("이름") && doc.Category == EMemoryCategory.PlayerName)
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