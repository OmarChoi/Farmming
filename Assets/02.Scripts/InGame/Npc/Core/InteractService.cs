using UnityEngine;

public class InteractService : MonoBehaviour
{
    [Header("컨트롤러")]
    [SerializeField] private NpcDialogueController _dialogueController;
    [SerializeField] private AiDialogueController _aiDialogueController;
    [SerializeField] private ShopController _shopController;

    private IDialogueHandler _scriptedHandler;
    private IDialogueHandler _aiHandler;

    private void Awake()
    {
        if (_dialogueController == null)
        {
            _dialogueController = FindFirstObjectByType<NpcDialogueController>();
        }
        if (_aiDialogueController == null)
        {
            _aiDialogueController = FindFirstObjectByType<AiDialogueController>();
        }
        if (_shopController == null)
        {
            _shopController = FindFirstObjectByType<ShopController>();
        }

        _scriptedHandler = new ScriptedDialogueHandler(_dialogueController);
        _aiHandler = new AIDialogueHandler(_aiDialogueController);
    }

    public void Execute(ENpcInteractionType type, NpcInteractionContext context)
    {
        if (context == null || context.Npc == null) return;

        switch (type)
        {
            case ENpcInteractionType.Talk:
                _scriptedHandler.StartDialogue(context, false);
                break;

            case ENpcInteractionType.DeepTalk:
                StartDeepTalk(context);
                break;

            case ENpcInteractionType.Trade:
                OpenTrade(context);
                break;

            case ENpcInteractionType.Upgrade:
                ExecuteUpgrade(context);
                break;

            case ENpcInteractionType.Quest:
                ExecuteQuest(context);
                break;

            case ENpcInteractionType.EndTalk:
                EndInteraction(context);
                break;
        }
    }

    public void StartGreeting(NpcInteractionContext context)
    {
        if (context == null || context.Npc == null) return;
        _scriptedHandler.StartDialogue(context, true);
    }

    private void StartDeepTalk(NpcInteractionContext context)
    {
        if (!HasInteractionOption(context, ENpcInteractionType.DeepTalk)) return;

        _dialogueController.PrepareForDeepTalk();
        _aiHandler.StartDialogue(context, true);
    }

    private void OpenTrade(NpcInteractionContext context)
    {
        if (_shopController == null || context.Npc.Shop == null) return;

        _dialogueController.Close();
        _shopController.OpenShop(context.Npc.Shop, context);
    }

    private void EndInteraction(NpcInteractionContext context)
    {
        context.InteractionComponent?.EndInteraction();
    }

    private bool HasInteractionOption(NpcInteractionContext context, ENpcInteractionType type)
    {
        var options = context.Npc.InteractionOptions;
        if (options == null) return false;

        foreach (var option in options)
        {
            if (option.Type == type) return true;
        }

        return false;
    }

    private void ExecuteUpgrade(NpcInteractionContext context)
    {
        // todo.업그레이드 기능 연결
    }

    private void ExecuteQuest(NpcInteractionContext context)
    {
        // todo.npc 전용 퀘스트 연결
    }
}
