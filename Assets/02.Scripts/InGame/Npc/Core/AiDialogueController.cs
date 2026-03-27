using UnityEngine;
using Cysharp.Threading.Tasks;
using LLMUnity;

public class AiDialogueController : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    [SerializeField] private UI_NpcAiDialogue _uiDialogue;
    [SerializeField] private LLMAgent _llmAgent;

    private readonly NpcPromptBuilder _promptBuilder = new();

    private NpcInteractionContext _currentContext;
    private bool _isSessionOpen;
    private bool _isGenerating;

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
        if (context == null || context.Npc == null || _uiDialogue == null || _llmAgent == null) return;
        if (_isSessionOpen)
        {
            await CloseSessionAsync(false);
        }

        _currentContext = context;
        _isSessionOpen = true;
        _isGenerating = false;

        _uiDialogue.Open(context.NpcName);
        _uiDialogue.ClearMessages();
        _uiDialogue.SetGenerating(false);
        _uiDialogue.AddSystemMessage("대화할 준비 중이에요.");

        _llmAgent.CancelRequests();
        await _llmAgent.ClearHistory();

        var openRequest = BuildRequest(context, string.Empty, true);
        _llmAgent.systemPrompt = _promptBuilder.BuildSystemPrompt(openRequest);

        await _llmAgent.Warmup();

        _uiDialogue.AddSystemMessage("준비가 끝났어요.");
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

        try
        {
            var req = BuildRequest(_currentContext, playerInput, false);

            string reply = await _llmAgent.Chat(
                req.PlayerInput,
                partial => _uiDialogue.UpdateNpcStreaming(Sanitize(partial)),
                null,
                true
            );

            _uiDialogue.CompleteNpcStreaming(Sanitize(reply));
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            _uiDialogue.CompleteNpcStreaming("...... (생각에 잠겨 있다.)");
        }
        finally
        {
            _isGenerating = false;
            _uiDialogue.SetGenerating(false);
        }
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
        if (_llmAgent != null)
        {
            _llmAgent.CancelRequests();
            await _llmAgent.ClearHistory();
        }

        var context = _currentContext;

        _isGenerating = false;
        _isSessionOpen = false;
        _currentContext = null;

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

    private NpcAiDialogueRequest BuildRequest(NpcInteractionContext context, string playerInput, bool isGreeting)
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
            Friendship = GetFriendship(context),
            FriendshipStep = GetFriendshipStep(context)
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

    private int GetFriendship(NpcInteractionContext context) => 0;
    private string GetFriendshipStep(NpcInteractionContext context) => "Awkward";

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
