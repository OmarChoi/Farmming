using UnityEngine;

public class NpcInteractionComponent : MonoBehaviour, INpcInteraction
{
    [SerializeField] private NpcDialogueController _dialogueController;
    private NpcController _npcController;

    private void Awake()
    {
        _npcController = GetComponent<NpcController>();
        if (_dialogueController == null)
        {
            _dialogueController = FindFirstObjectByType<NpcDialogueController>();
        }
    }

    // todo. 플레이어가 NPC와 상호작용할 때 이 메서드를 호출하면 됩니다.
    public void RequestInteract(Transform interactor)
    {
        if (!_npcController.CanStartInteraction(interactor))
        {
            return;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _npcController.StartInteraction(interactor);

        _dialogueController.Open(_npcController, interactor);
    }

    public void EndInteraction()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _dialogueController.Close();
        _npcController.EndInteraction();
    }
}
