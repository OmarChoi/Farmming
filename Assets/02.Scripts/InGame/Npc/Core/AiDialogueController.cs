using Cysharp.Threading.Tasks;
using LLMUnity;
using UnityEngine;

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
            await CloseSessionAsync();
        }

        _currentContext = context;
        _isSessionOpen = true;
        _isGenerating = false;

        _uiDialogue.Open(context.NpcName);
        _uiDialogue.ClearMessages();
        _uiDialogue.SetGenerating(false);

        await _llmAgent.ClearHistory();

        NpcAiDialogueRequest openRequest = BuildRequest(context, string.Empty, true);
        _llmAgent.systemPrompt = _promptBuilder.BuildSystemPrompt(openRequest);

        await _llmAgent.Warmup();

        _uiDialogue.AddSystemMessage("깊은 대화가 시작되었습니다.");
    }

    private void HandleSendRequested(string playerInput)
    {
        if (!_isSessionOpen || _isGenerating) return;

        if (string.IsNullOrWhiteSpace(playerInput)) return;

        SendPlayerMessageAsync(playerInput).Forget();
    }

    private async UniTask SendPlayerMessageAsync(string playerInput)
    {
        if (_currentContext == null || _llmAgent == null || _uiDialogue == null) return;

        _isGenerating = true;
        _uiDialogue.SetGenerating(true);
        _uiDialogue.AddPlayerMessage(playerInput);
        _uiDialogue.ClearInputField();
        _uiDialogue.BeginNpcStreaming();

        try
        {
            NpcAiDialogueRequest req = BuildRequest(_currentContext, playerInput, false);

            string reply = await _llmAgent.Chat(
                req.PlayerInput,
                partial =>
                {
                    _uiDialogue.UpdateNpcStreaming(Sanitize(partial));
                },
                null,
                true
            );

            _uiDialogue.CompleteNpcStreaming(Sanitize(reply));
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            _uiDialogue.CompleteNpcStreaming("……잠깐 생각이 많아졌네. 다시 한 번 말해줄래?");
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
        CloseSessionAsync().Forget();
    }

    public async UniTask CloseSessionAsync()
    {
        if (_llmAgent != null)
        {
            _llmAgent.CancelRequests();
            await _llmAgent.ClearHistory();
        }

        _isGenerating = false;
        _isSessionOpen = false;
        _currentContext = null;

        if (_uiDialogue != null)
        {
            _uiDialogue.SetGenerating(false);
            _uiDialogue.Close();
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
            Job = context.Npc.Data.Job,
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
        return $"현재 NPC 상태: 대화 중, 플레이어와 마주 보고 있음.";
    }

    private string GetPlayerId(NpcInteractionContext context)
    {
        return context.Interactor != null ? context.Interactor.name : "UnknownPlayer";
    }

    private int GetFriendship(NpcInteractionContext context)
    {
        return 0;
    }

    private string GetFriendshipStep(NpcInteractionContext context)
    {
        return "Awkward";
    }

    private string Sanitize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "……";

        text = text.Trim();
        text = text.Replace("\r", " ").Replace("\n", " ");
        return text;
    }
}
