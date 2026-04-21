using Cysharp.Threading.Tasks;
using UnityEngine;

public class DinoRaceInteractionService : MonoBehaviour
{
    private UI_DinoRace _ui;
    private NpcInteractionContext _currentContext;
    private DinoRaceHost _currentHost;

    public void BeginInteraction(NpcInteractionContext context)
    {
        if (context == null || context.Npc == null) return;
        UI_DinoRace ui = UIController.Instance.GetInstance<UI_DinoRace>();
        if (ui.IsOpen) return;
        
        DinoRaceNpcFeature feature = context.Npc.GetComponent<DinoRaceNpcFeature>();
        if (feature == null || feature.Host == null)
        {
            Debug.LogWarning("[DinoRaceInteractionService] NPC is not bound to a dino race host.");
            return;
        }

        ClearCurrentBinding(false);

        _currentContext = context;
        _currentHost = feature.Host;

        NpcDialogueController.Instance?.Close();

        DinoRaceSettings settings = _currentHost.Settings ?? new DinoRaceSettings();
        OpenUI(settings).Forget();
    }

    private async UniTaskVoid OpenUI(DinoRaceSettings settings)
    {
        int currentGold = CurrencyManager.Instance != null
            ? (int)CurrencyManager.Instance.GetGold()
            : 0;
        
        _ui = await UIController.Instance.OpenAsync
        (
            new UILifecycleActions<UI_DinoRace>
            {
                OnOpen = ui => ui.OpenSelection(
                    _currentHost.GetRunnerDisplayNames(),
                    currentGold,
                    settings.DefaultBetAmount,
                    settings.BetStepAmount,
                    settings.MinBetAmount,
                    HandleSelectionConfirmed,
                    HandleSelectionCancelled)
            }
        );
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
        
        NpcInteractionComponent interactionComponent = _currentContext?.InteractionComponent;
        ClearCurrentBinding(true);
        interactionComponent?.EndInteraction();
    }

    private void ClearCurrentBinding(bool clearUi)
    {
        if (clearUi)
        {
            UIController.Instance.CloseAsync<UI_DinoRace>().Forget();
            _ui = null;
        }
        
        _currentHost = null;
        _currentContext = null;
    }
}