using UnityEngine;

public class NpcInteractionComponent : MonoBehaviour, INpcInteraction
{
    [SerializeField] private NpcDialogueController _dialogueController;
    private NpcController _npcController;
    private PlayerNPCInteractionAbility _playerInteraction;

    private void Awake()
    {
        _npcController = GetComponent<NpcController>();
        if (_dialogueController == null)
        {
            _dialogueController = FindFirstObjectByType<NpcDialogueController>();
        }
    }

    public void RequestInteract(Transform interactor)
    {
        if (!_npcController.CanStartInteraction(interactor))
        {
            return;
        }

        _playerInteraction = interactor.GetComponentInChildren<PlayerNPCInteractionAbility>();

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
        _playerInteraction?.EndInteraction();
        _playerInteraction = null;
    }
}
