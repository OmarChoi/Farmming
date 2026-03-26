using UnityEngine;

public class InteractService : MonoBehaviour
{
    [SerializeField] private NpcDialogueController _dialogueController;
    [SerializeField] private AiDialogueController _aiDialogueController;

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

        _scriptedHandler = new ScriptedDialogueHandler(_dialogueController);
        _aiHandler = new AIDialogueHandler(_aiDialogueController);
    }

    public void Execute(ENpcInteractionType type, NpcInteractionContext context)
    {
        if (context == null || context.Npc == null) return;

        switch (type)
        {
            case ENpcInteractionType.Talk:
                ExecuteScriptedTalk(context, true);
                break;

            case ENpcInteractionType.DeepTalk:
                if (!HasInteractionOption(context, ENpcInteractionType.DeepTalk))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[{context.NpcName}] 는 깊은 대화를 지원하지 않습니다.");
#endif
                    return;
                }

                _aiHandler.StartDialogue(context, true);
                break;

            case ENpcInteractionType.Trade:
                ExecuteTrade(context);
                break;

            case ENpcInteractionType.Upgrade:
                ExecuteUpgrade(context);
                break;

            case ENpcInteractionType.Quest:
                ExecuteQuest(context);
                break;

            case ENpcInteractionType.EndTalk:
                ExecuteEndTalk(context);
                break;
        }
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

    public void ExecuteScriptedTalk(NpcInteractionContext context, bool isStart)
    {
        if (context == null || context.Npc == null) return;

        _scriptedHandler.StartDialogue(context, isStart);
    }

    private void ExecuteTrade(NpcInteractionContext context)
    {
        if (context.Npc.Shop == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("연결된 상점 데이터가 없습니다.");
#endif
            return;
        }

        ShopController shopController = Object.FindFirstObjectByType<ShopController>();
        if (shopController == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("ShopController를 찾지 못했습니다.");
#endif
            return;
        }
        shopController.OpenShop(context.Npc.Shop, context);
        _dialogueController.Close();
    }
    private void ExecuteUpgrade(NpcInteractionContext context)
    {
        // todo.업그레이드 기능 연결
    }
    private void ExecuteQuest(NpcInteractionContext context)
    {
        // todo.npc 전용 퀘스트 연결
    }
    private void ExecuteEndTalk(NpcInteractionContext context)
    {
        context.InteractionComponent.EndInteraction();
    }
}
