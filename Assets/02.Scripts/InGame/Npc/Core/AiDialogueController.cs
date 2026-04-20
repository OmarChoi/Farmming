using Cysharp.Threading.Tasks;
using LLMUnity;
using System;
using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;

public class AiDialogueController : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    [SerializeField] private UI_NpcAiDialogue _uiDialogue;
    [SerializeField] private LLMAgent _llmAgent;
    [SerializeField] private NpcMemoryLlmRagService _ragService;

    [Header("RAG에서 가져올 기억 개수")]
    [SerializeField] private int _memoryTopK = 4;

    private readonly NpcPromptBuilder _promptBuilder = new();
    private NpcMemoryService _memoryService;
    private readonly NpcMemoryExtractor _memoryExtractor = new();
    private readonly NpcSessionSummarizer _summarizer = new();
    private readonly NpcMemoryUpdater _memoryUpdater = new();
    private readonly NpcMemoryDecaySystem _memoryDecaySystem = new();

    private NpcInteractionContext _currentContext;
    private NpcMemoryProfile _currentProfile;
    private readonly List<DialogueTurnRecord> _sessionTurns = new();
    private INpcMemoryRepository _repository;

    private bool _isSessionOpen;
    private bool _isGenerating;

    private void Awake()
    {
        if (_uiDialogue == null)
        {
            _uiDialogue = FindFirstObjectByType<UI_NpcAiDialogue>();
        }
        if (_llmAgent == null)
        {
            _llmAgent = FindFirstObjectByType<LLMAgent>();
        }
        if (_ragService == null)
        {
            _ragService = FindFirstObjectByType<NpcMemoryLlmRagService>();
        }
        if (_repository == null)
        {
            _repository = new JsonNpcMemoryRepository();
        }

        _memoryService = new NpcMemoryService(_repository, _ragService);
    }
    private async void Start()
    {
        if (_ragService != null)
        {
            await _ragService.InitializeMemoryRagAsync();
        }
    }

    private void OnEnable()
    {
        if (_uiDialogue == null) return;

        _uiDialogue.OnSendRequested += HandleSendRequested;
        _uiDialogue.OnStopRequested += HandleStopRequested;
        _uiDialogue.OnCloseRequested += HandleCloseRequested;
    }

    private void OnDisable()
    {
        if (_uiDialogue == null) return;

        _uiDialogue.OnSendRequested -= HandleSendRequested;
        _uiDialogue.OnStopRequested -= HandleStopRequested;
        _uiDialogue.OnCloseRequested -= HandleCloseRequested;
    }

    public async UniTask OpenSessionAsync(NpcInteractionContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (context.Npc == null) throw new ArgumentNullException(nameof(context.Npc));
        if (_uiDialogue == null) throw new InvalidOperationException("UIDialogue가 없습니다.");
        if (_llmAgent == null) throw new InvalidOperationException("LLMAgent가 없습니다.");

        if (_isSessionOpen)
        {
            await CloseSessionAsync(false);
        }

        _currentContext = context;
        _isSessionOpen = true;
        _isGenerating = false;
        _sessionTurns.Clear();

        string npcId = context.Npc.Data.NpcId;
        string playerId = GetPlayerId(context);

        _currentProfile = await _memoryService.LoadProfileAsync(npcId, playerId);

        _uiDialogue.Open(context.NpcName);
        _uiDialogue.SetVoice(context.Npc.Voice);
        _uiDialogue.ClearMessages();
        _uiDialogue.SetGenerating(false);
        _uiDialogue.AddSystemMessage("대화할 준비 중이에요.");

        _llmAgent.CancelRequests();
        await _llmAgent.ClearHistory();

        var openRequest = BuildRequest(context, string.Empty, true, new List<string>());
        _llmAgent.systemPrompt = _promptBuilder.BuildSystemPrompt(openRequest);

        await RestoreRecentTurnsAsync(_currentProfile);
        await _llmAgent.Warmup();

        _uiDialogue.ClearMessagesExcludePlayer();
        _uiDialogue.AddSystemMessage("준비가 끝났어요.");
    }

    private async UniTask RestoreRecentTurnsAsync(NpcMemoryProfile profile)
    {
        if (profile == null || profile.RecentTurns == null) return;

        foreach (var turn in profile.RecentTurns)
        {
            if (turn.Role == "user")
            {
                await _llmAgent.AddUserMessage(turn.Text);
            }
            else if (turn.Role == "assistant")
            {
                await _llmAgent.AddAssistantMessage(turn.Text);
            }
        }

        if (!string.IsNullOrWhiteSpace(profile.RollingSummary))
        {
            _llmAgent.SetSummary(profile.RollingSummary);
        }
    }

    private void HandleSendRequested(string playerInput)
    {
        if (!_isSessionOpen || _isGenerating || string.IsNullOrWhiteSpace(playerInput)) return;
        SendPlayerMessageAsync(playerInput).Forget();
    }

    private async UniTask SendPlayerMessageAsync(string playerInput)
    {
        if (_currentContext == null || _llmAgent == null || _uiDialogue == null) return;

        _isGenerating = true;
        _uiDialogue.ClearMessagesExcludePlayer();
        _uiDialogue.SetGenerating(true);
        _uiDialogue.AddPlayerMessage(playerInput);
        _uiDialogue.ClearInputField();
        _uiDialogue.BeginNpcStreaming();

        _sessionTurns.Add(new DialogueTurnRecord{Role = "user", Text = playerInput, Ticks = DateTime.UtcNow.Ticks});

        try
        {
            var relevantMemories = await _memoryService.SearchRelevantAsync(
                _currentProfile.NpcId,
                _currentProfile.PlayerId,
                playerInput,
                _memoryTopK);

            var req = BuildRequest(_currentContext, playerInput, false, relevantMemories);

            // 매 턴 기억 검색 결과가 달라지므로 시스템 프롬프트를 갱신합니다.
            _llmAgent.systemPrompt = _promptBuilder.BuildSystemPrompt(req);

            string reply = await _llmAgent.Chat(
                req.PlayerInput,
                partial => _uiDialogue.UpdateNpcStreaming(Sanitize(partial)),
                null,
                true
            );

            string sanitizedReply = Sanitize(reply);

            _sessionTurns.Add(new DialogueTurnRecord{Role = "assistant", Text = sanitizedReply, Ticks = DateTime.UtcNow.Ticks});

            _uiDialogue.CompleteNpcStreaming(sanitizedReply);
        }
        catch (OperationCanceledException)
        {
            _uiDialogue.MarkStreamingStopped();
        }
        catch (HttpRequestException)
        {
            Debug.LogError("Network error during chat.");
            HandleFallback("서버와 연결이 불안정해요.");
        }
        catch (TimeoutException)
        {
            Debug.LogError("Chat timeout.");
            HandleFallback("응답이 너무 늦어지고 있어요.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            HandleFallback("...... (생각에 잠겨 있다.)");
        }
        finally
        {
            _isGenerating = false;
            _uiDialogue.SetGenerating(false);
        }
    }

    private void HandleFallback(string message)
    {
        _sessionTurns.Add(new DialogueTurnRecord
        {
            Role = "assistant",
            Text = message,
            Ticks = DateTime.UtcNow.Ticks
        });

        _uiDialogue.CompleteNpcStreaming(message);
    }

    private void HandleStopRequested()
    {
        if (!_isSessionOpen || !_isGenerating || _llmAgent == null || _uiDialogue == null) return;

        _llmAgent.CancelRequests();
        _isGenerating = false;
        _uiDialogue.SetGenerating(false);
        _uiDialogue.MarkStreamingStopped();
    }

    private void HandleCloseRequested()
    {
        CloseSessionAndInteractionAsync().Forget();
    }

    public async UniTask CloseSessionAndInteractionAsync()
    {
        await CloseSessionAsync(true);
    }

    private async UniTask CloseSessionAsync(bool endInteraction)
    {
        if (_currentProfile != null)
        {
            string summary = _summarizer.Summarize(_sessionTurns);

            List<NpcMemoryEntry> extracted = _memoryExtractor.Extract(
                _currentProfile.NpcId,
                _currentProfile.PlayerId,
                _sessionTurns);

            _memoryUpdater.Apply(_currentProfile, extracted);
            _memoryDecaySystem.ApplyDecay(_currentProfile);
            await _memoryService.SaveAfterDialogueAsync(_currentProfile, _sessionTurns, summary);
        }
        if (_llmAgent != null)
        {
            _llmAgent.CancelRequests();
            await _llmAgent.ClearHistory();
        }

        var context = _currentContext;

        _isGenerating = false;
        _isSessionOpen = false;
        _currentContext = null;
        _currentProfile = null;
        _sessionTurns.Clear();

        if (_uiDialogue != null)
        {
            _uiDialogue.SetGenerating(false);
            _uiDialogue.Close();
        }

        if (endInteraction)
        {
            context?.InteractionComponent?.EndInteraction();
        }
    }

    private NpcAiDialogueRequest BuildRequest(NpcInteractionContext context, string playerInput, bool isGreeting, List<string> relevantMemories)
    {
        return new NpcAiDialogueRequest
        {
            NpcId = context.Npc.Data.NpcId,
            NpcName = context.Npc.Data.NpcName,
            Mbti = context.Npc.Data.NpcMbti,
            Personality = context.Npc.Data.NpcPersonality,
            Age = context.Npc.Data.Age,
            Job = context.Npc.Data.Job,
            WayOfTone = context.Npc.Data.WayOfTone,
            Context = BuildContext(context),
            PlayerInput = playerInput,
            PlayerId = GetPlayerId(context),
            IsGreeting = isGreeting,
            Friendship = _currentProfile?.Friendship ?? 0,
            FriendshipStep = _currentProfile?.FriendshipStep ?? "Awkward",
            RollingSummary = _currentProfile?.RollingSummary ?? string.Empty,
            RelevantMemories = relevantMemories ?? new List<string>()
        };
    }

    private string BuildContext(NpcInteractionContext context)
    {
        return "현재 NPC 상태: 대화 중, 플레이어와 마주 보고 있다.";
    }

    private string GetPlayerId(NpcInteractionContext context)
    {
        return context.Interactor != null ? context.Interactor.name : "UnknownPlayer";
    }

    private string Sanitize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "……";
        }

        text = text.Trim();
        text = text.Replace("\r", " ").Replace("\n", " ");
        return text;
    }
}
