using UnityEngine;

public class InteractService : MonoBehaviour
{
    [SerializeField] private UI_NpcDialogue _uiDialogue;

    private void Awake()
    {
        if (_uiDialogue == null)
        {
            _uiDialogue = Object.FindFirstObjectByType<UI_NpcDialogue>();
        }
    }

    public void Execute(ENpcInteractionType type, NpcInteractionContext context)
    {
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
        string name = context.NpcName;
        int dialogueNumber = Random.Range(0, context.Npc.Data.TalkDialogues.Length);
        string dialogue = context.Npc.Data.TalkDialogues[dialogueNumber];
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
