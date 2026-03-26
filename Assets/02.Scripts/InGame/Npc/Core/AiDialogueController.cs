using UnityEngine;
using Cysharp.Threading.Tasks;
using LLMUnity;

public class AiDialogueController : MonoBehaviour
{
    [SerializeField] private LLMAgent _agent;

    private NpcPromptBuilder _promptBuilder;

    private NpcInteractionContext _currentContext;
    private bool _isSessionOpen;
    private bool _isGenerating;

    private void Awake()
    {
        _promptBuilder = new NpcPromptBuilder();
    }

    public async UniTask OpenSessionAsync(NpcInteractionContext context)
    {
        if (context == null || context.Npc == null || _agent == null) return;

        _currentContext = context;
        _isSessionOpen = true;
        _isGenerating = false;

        await _agent.ClearHistory();

        NpcAiDialogueRequest openRequest = BuildOpenRequest(context);
        _agent.systemPrompt = _promptBuilder.BuildSystemPrompt(openRequest);

        await _agent.Warmup();
    }

    private void HandleSendRequested(string playerInput)
    {
        if (!_isSessionOpen || _isGenerating) return;

        if (string.IsNullOrWhiteSpace(playerInput)) return;

        SendPlayerMessageAsync(playerInput).Forget();
    }

    private async UniTask SendPlayerMessageAsync(string playerInput)
    {
        _isGenerating = true;

        try
        {
            NpcAiDialogueRequest req = BuildChatRequest(_currentContext, playerInput);

            string reply = await _agent.Chat(
                req.PlayerInput,
                partial =>
                {
                    // _ui.UpdateNpcStreaming(Sanitize(partial));
                },
                null,
                true
            );
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            _isGenerating = false;
        }
    }

    private void HandleStopRequested()
    {
        if (!_isGenerating || _agent == null) return;

        _agent.CancelRequests();
        _isGenerating = false;
    }

    private void HandleCloseRequested()
    {
        CloseSessionAsync().Forget();
    }

    public async UniTask CloseSessionAsync()
    {
        if (_agent != null)
        {
            _agent.CancelRequests();
            await _agent.ClearHistory();
        }

        _isGenerating = false;
        _isSessionOpen = false;
        _currentContext = null;
    }

    private NpcAiDialogueRequest BuildOpenRequest(NpcInteractionContext context)
    {
        return new NpcAiDialogueRequest
        {
            NpcId = context.Npc.Data.NpcId,
            NpcName = context.Npc.Data.NpcName,
            Mbti = context.Npc.Data.NpcMbti,
            Personality = context.Npc.Data.NpcPersonality,
            Job = context.Npc.Data.Job,
            Context = BuildContext(context),
            PlayerInput = "",
            PlayerId = GetPlayerId(context),
            IsGreeting = true,
            Friendship = GetFriendship(context),
            FriendshipStep = GetFriendshipStep(context)
        };
    }

    private NpcAiDialogueRequest BuildChatRequest(NpcInteractionContext context, string playerInput)
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
            IsGreeting = false,
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
        if (string.IsNullOrWhiteSpace(text)) return "……";

        text = text.Trim();
        text = text.Replace("\r", " ").Replace("\n", " ");

        return text;
    }
}
