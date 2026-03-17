using UnityEngine;

public class NpcInteractionComponent : MonoBehaviour, INpcInteraction
{
    private NpcController _npcController;

    private void Awake()
    {
        _npcController = GetComponent<NpcController>();
    }

    // todo. 플레이어가 NPC와 상호작용할 때 이 메서드를 호출하면 됩니다.
    public void RequestInteract(Transform interactor)
    {
        if (!_npcController.CanStartInteraction(interactor))
        {
            return;
        }

        _npcController.StartInteraction(interactor);

        UI_NpcDialogue.Instance.Open(_npcController, interactor);
    }

    public void EndInteraction()
    {
        UI_NpcDialogue.Instance.Close();
        _npcController.EndInteraction();
    }
}
