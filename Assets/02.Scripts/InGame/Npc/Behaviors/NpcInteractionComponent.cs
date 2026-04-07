using UnityEngine;

public class NpcInteractionComponent : MonoBehaviour, INpcInteraction
{
    [Header("NpcDialogueController")]
    [SerializeField] private NpcDialogueController _dialogueController;

    private NpcController _npcController;
    private PlayerController _playerController;
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

        _playerController = interactor.GetComponentInParent<PlayerController>();
        _playerInteraction = interactor.GetComponentInChildren<PlayerNPCInteractionAbility>();

        _playerController?.SetCursorLock(false);
        _npcController.StartInteraction(interactor);

        _dialogueController.Open(_npcController, interactor);
    }

    public void EndInteraction()
    {
        _playerController?.SetCursorLock(true);

        _dialogueController.Close();
        _npcController.EndInteraction();
        _playerInteraction?.EndInteraction();
        _playerInteraction = null;
        _playerController = null;
    }
}
