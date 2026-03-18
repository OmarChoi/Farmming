using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

public class InteractService : MonoBehaviour
{
    [SerializeField] private UI_NpcDialogue _uiDialogue;
    private DialogueHandlerSelector _handlerSelector;

    private void Awake()
    {
        if (_uiDialogue == null)
        {
            _uiDialogue = FindFirstObjectByType<UI_NpcDialogue>();
        }
        // var aiController = FindFirstObjectByType<AIChatController>();

        var scripted = new ScriptedDialogueHandler(_uiDialogue);
        var ai = new ScriptedDialogueHandler(_uiDialogue);
        // var ai = new AIDialogueHandler(aiController); ai는 추후 이런 식으로 변경할 예정입니다.

        _handlerSelector = new DialogueHandlerSelector(ai, scripted);
    }

    public void Execute(ENpcInteractionType type, NpcInteractionContext context)
    {
        if (_uiDialogue == null)
        {
            _uiDialogue = FindFirstObjectByType<UI_NpcDialogue>();
        }

        switch (type)
        {
            case ENpcInteractionType.Talk:
                ExecuteNormalTalk(context);
                break;

            case ENpcInteractionType.Trade:
                ExecuteTrade(context);
                break;

            case ENpcInteractionType.EndTalk:
                ExecuteEndTalk(context);
                break;
        }
    }

    public void ExecuteStartTalk(NpcInteractionContext context)
    {
        var handler = _handlerSelector.Resolve();
        handler.StartDialogue(context, true);
    }

    public void ExecuteNormalTalk(NpcInteractionContext context)
    {
        var handler = _handlerSelector.Resolve();
        handler.StartDialogue(context, false);
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
        _uiDialogue.SetActiveFalse();
    }

    private void ExecuteEndTalk(NpcInteractionContext context)
    {
        context.InteractionComponent.EndInteraction();
    }
}
