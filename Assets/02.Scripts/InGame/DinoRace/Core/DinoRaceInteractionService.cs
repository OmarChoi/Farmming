using UnityEngine;

public class DinoRaceInteractionService : MonoBehaviour
{
    [SerializeField] private NpcDialogueController _dialogueController;
    [SerializeField] private UI_DinoRace _ui;

    private NpcInteractionContext _currentContext;
    private DinoRaceHost _currentHost;

    private void Awake()
    {
        if (_dialogueController == null)
            _dialogueController = FindFirstObjectByType<NpcDialogueController>();
        if (_ui == null)
            _ui = FindFirstObjectByType<UI_DinoRace>(FindObjectsInactive.Include);
    }

    public void BeginInteraction(NpcInteractionContext context)
    {
        if (context == null || context.Npc == null || _ui == null)
            return;

        DinoRaceNpcFeature feature = context.Npc.GetComponent<DinoRaceNpcFeature>();
        if (feature == null || feature.Host == null)
        {
            Debug.LogWarning("[DinoRaceInteractionService] NPC is not bound to a dino race host.");
            return;
        }

        ClearCurrentBinding(false);

        _currentContext = context;
        _currentHost = feature.Host;

        _dialogueController?.Close();

        int currentGold = CurrencyManager.Instance != null
            ? (int)CurrencyManager.Instance.GetGold()
            : 0;

        DinoRaceSettings settings = _currentHost.Settings ?? new DinoRaceSettings();
        _ui.OpenSelection(
            _currentHost.GetRunnerDisplayNames(),
            currentGold,
            settings.DefaultBetAmount,
            settings.BetStepAmount,
            settings.MinBetAmount,
            HandleSelectionConfirmed,
            HandleSelectionCancelled);
    }

    private void HandleSelectionConfirmed(int selectedRunnerIndex, int betAmount)
    {
        if (_currentHost == null || _ui == null) return;

        if (!_currentHost.TryBeginRace(selectedRunnerIndex, betAmount, out string error))
        {
            _ui.SetSelectionMessage(error);
            return;
        }

        EndCurrentInteraction(false);
    }

    private void HandleSelectionCancelled()
    {
        EndCurrentInteraction(false);
    }

    private void EndCurrentInteraction(bool resetRace)
    {
        if (resetRace && _currentHost != null)
            _currentHost.ResetRace();

        _ui?.Close();

        NpcInteractionComponent interactionComponent = _currentContext?.InteractionComponent;
        ClearCurrentBinding(true);
        interactionComponent?.EndInteraction();
    }

    private void ClearCurrentBinding(bool clearUi)
    {
        if (clearUi)
            _ui?.Close();

        _currentHost = null;
        _currentContext = null;
    }
}
