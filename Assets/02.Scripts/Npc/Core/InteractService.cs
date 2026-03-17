using UnityEngine;

public class InteractService : MonoBehaviour
{
    [SerializeField] private UI_NpcDialogue _uiDialogue;

    public void Execute(ENpcInteractionType type, NpcInteractionContext context)
    {
        if (_uiDialogue == null)
        {
            _uiDialogue = FindFirstObjectByType<UI_NpcDialogue>();
        }

        switch (type)
        {
            case ENpcInteractionType.Talk:
                ExecuteTalk(context);
                break;

            case ENpcInteractionType.Trade:
                ExecuteTrade(context);
                break;

            case ENpcInteractionType.EndTalk:
                ExecuteEndTalk(context);
                break;
        }
    }

    private void ExecuteTalk(NpcInteractionContext context)
    {
        int dialogueNumber = Random.Range(0, context.Npc.Data.TalkDialogues.Length);
        string dialogue = context.Npc.Data.TalkDialogues[dialogueNumber];
        _uiDialogue.UpdateDialogueText(dialogue);
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

        shopController.OpenShop(context.Npc.Shop);
    }

    private void ExecuteEndTalk(NpcInteractionContext context)
    {
        context.InteractionComponent.EndInteraction();
    }
}
