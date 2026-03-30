using UnityEngine;
using Cysharp.Threading.Tasks;
using LLMUnity;
using System;
using System.Collections.Generic;
using System.Linq;

public class NpcMemoryLlmRagService : MonoBehaviour, INpcMemoryRagService
{
    [SerializeField] private LLM _embeddingLlm;
    [SerializeField] private RAG _rag;

    private readonly Dictionary<string, List<NpcMemoryRagDocument>> _docsByKey = new();
    private bool _isInitialized;

    private const float PRIORITY_MAX = 100f;
    private const float PRIORITY_WEIGHT = 0.25f;

    private const float CATEGORY_SCORE_NAME = 0.8f;
    private const float CATEGORY_SCORE_PREFERENCE = 0.7f;
    private const float CATEGORY_SCORE_PROMISE = 0.7f;
    private const float CATEGORY_SCORE_EPISODE = 0.5f;

    private const int MIN_TOKEN_LENGTH = 2;
    private const float TOKEN_MATCH_SCORE = 0.2f;
    private const float TEXT_MATCH_MAX_SCORE = 0.8f;

    private static readonly TimeSpan VERY_RECENT_THRESHOLD = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan RECENT_THRESHOLD = TimeSpan.FromHours(1);
    private static readonly TimeSpan OLD_THRESHOLD = TimeSpan.FromDays(1);

    private const float RECENT_SCORE_VERY_RECENT = 0.25f;
    private const float RECENT_SCORE_RECENT = 0.18f;
    private const float RECENT_SCORE_OLD = 0.10f;
    private const float RECENT_SCORE_VERY_OLD = 0.03f;


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
        if (_isInitialized) return;

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
                Content = entry.Text,
                Priority = entry.Priority,
                UpdatedAtTicks = entry.UpdatedAtTicks,
                Embedding = null
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

        var scored = new List<(NpcMemoryRagDocument Doc, float Score)>();

        foreach (var doc in docs)
        {
            float score = CalculateScore(query, doc);

            if (score > 0f)
            {
                scored.Add((doc, score));
            }
        }

        var result = scored
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Doc.UpdatedAtTicks)
            .Take(topK)
            .Select(x => FormatMemoryForPrompt(x.Doc))
            .ToList();

        await UniTask.Yield();
        return result;
    }

    private float CalculateScore(string query, NpcMemoryRagDocument doc)
    {
        if (string.IsNullOrWhiteSpace(query) || doc == null || string.IsNullOrWhiteSpace(doc.Content))
        {
            return 0f;
        }

        string normalizedQuery = Normalize(query);
        string normalizedDoc = Normalize(doc.Content);

        float score = 0f;

        // 1. 카테고리 힌트 점수
        score += GetCategoryHintScore(normalizedQuery, doc.Category);

        // 2. 텍스트 겹침 점수
        score += GetTextOverlapScore(normalizedQuery, normalizedDoc);

        // 3. 우선순위 점수
        score += Mathf.Clamp01(doc.Priority / PRIORITY_MAX) * PRIORITY_WEIGHT;

        // 4. 최신성 점수
        score += GetRecencyScore(doc.UpdatedAtTicks);

        return score;
    }

    private float GetCategoryHintScore(string query, EMemoryCategory category)
    {
        float score = 0f;

        if (ContainsAny(query, "이름", "불러", "누구", "성함"))
        {
            if (category == EMemoryCategory.PlayerName)
            {
                score += CATEGORY_SCORE_NAME;
            }
        }

        if (ContainsAny(query, "좋아", "싫어", "취향", "선호", "먹고 싶", "원하"))
        {
            if (category == EMemoryCategory.PlayerPreference)
            {
                score += CATEGORY_SCORE_PREFERENCE;
            }
        }

        if (ContainsAny(query, "약속", "기억해", "잊지", "꼭", "나중에"))
        {
            if (category == EMemoryCategory.Promise)
            {
                score += CATEGORY_SCORE_PROMISE;
            }
        }

        if (ContainsAny(query, "전에", "예전", "지난번", "기억", "저번"))
        {
            if (category == EMemoryCategory.Episode)
            {
                score += CATEGORY_SCORE_EPISODE;
            }
        }

        return score;
    }

    private float GetTextOverlapScore(string query, string content)
    {
        string[] queryTokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (queryTokens.Length == 0) return 0f;

        float score = 0f;

        foreach (string token in queryTokens)
        {
            if (token.Length < MIN_TOKEN_LENGTH) continue;

            if (content.Contains(token))
            {
                score += TOKEN_MATCH_SCORE;
            }
        }

        return Mathf.Min(score, TEXT_MATCH_MAX_SCORE);
    }

    private float GetRecencyScore(long updatedAtTicks)
    {
        long now = DateTime.UtcNow.Ticks;
        long diff = now - updatedAtTicks;

        if (diff <= VERY_RECENT_THRESHOLD.Ticks) return RECENT_SCORE_VERY_RECENT;
        if (diff <= RECENT_THRESHOLD.Ticks) return RECENT_SCORE_RECENT;
        if (diff <= OLD_THRESHOLD.Ticks) return RECENT_SCORE_OLD;

        return RECENT_SCORE_VERY_OLD;
    }

    private string FormatMemoryForPrompt(NpcMemoryRagDocument doc)
    {
        return $"[{doc.Category}] {doc.Content}";
    }

    private string Normalize(string text)
    {
        return text.Trim().ToLowerInvariant();
    }

    private bool ContainsAny(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (text.Contains(keyword)) return true;
        }

        return false;
    }

    private string MakeKey(string npcId, string playerId) => $"{npcId}::{playerId}";
}