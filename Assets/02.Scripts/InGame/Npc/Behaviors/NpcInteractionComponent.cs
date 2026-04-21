using UnityEngine;

public class NpcInteractionComponent : MonoBehaviour, IInteraction
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

    public void RequestInteract(PlayerController player)
    {
        if (player == null || !player.IsMine) return;

        if (!_npcController.CanStartInteraction(player)) return;

        _playerController = player;
        _playerInteraction = player.GetAbility<PlayerNPCInteractionAbility>();

        _playerController?.SetCursorLock(false);
        _npcController.StartInteraction(player);
        _dialogueController.Open(_npcController, player).Forget();
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
