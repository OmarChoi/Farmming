using UnityEngine;

public class InteractService : MonoBehaviour
{
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
        Debug.Log($"{context.Npc.name}와 대화를 시작합니다.");
        // 여기서 실제 대사 출력 시스템 연결
        // ex) UI_DialogueText.Instance.Show(...)
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
