using UnityEngine;
using System.Collections.Generic;

public class NpcCuringService : MonoBehaviour
{
    [Header("참조 컴포넌트")]
    [SerializeField] private NpcDialogueController _dialogueController;

    public void ExecuteCuringInteraction(NpcInteractionContext context)
    {
        if (context == null || context.Npc == null) return;

        NpcDataSO data = context.Npc.Data;
        if (data == null || data.CuringAskDialogue == null)
        {
            _dialogueController.ShowDefaultChoices();
            return;
        }

        _dialogueController.StartDialogue(data.CuringAskDialogue, EDialogueUiState.AskCuring,
            () =>
            {
                ShowCuringConfirmChoices(context);
                return true;
            });
    }

    private void ShowCuringConfirmChoices(NpcInteractionContext context)
    {
        var choices = new List<NpcDialogueChoiceData>
        {
            new NpcDialogueChoiceData(
                "네",
                () => OnSelectAccept(context)
            ),
            new NpcDialogueChoiceData(
                "아니오",
                () => OnSelectDecline(context)
            )
        };

        _dialogueController.ShowCustomChoices(choices);
    }

    private void OnSelectAccept(NpcInteractionContext context)
    {
        NpcDataSO data = context.Npc.Data;
        if (data == null)
        {
            _dialogueController.Close();
            return;
        }

        bool alreadyCuredToday = !CanReceiveCuringToday(context.Interactor);

        if (alreadyCuredToday)
        {
            if (data.CuringAlreadyDoneDialogue != null)
            {
                _dialogueController.StartDialogue(data.CuringAlreadyDoneDialogue, EDialogueUiState.CuringAlreadyDone);
            }
            else if (data.CuringDeclineDialogue != null)
            {
                _dialogueController.StartDialogue(data.CuringDeclineDialogue, EDialogueUiState.CuringDecline);
            }
            else
            {
                _dialogueController.ShowDefaultChoices();
            }

            return;
        }

        ApplyCuring(context.Interactor);

        if (data.CuringAcceptDialogue != null)
        {
            _dialogueController.StartDialogue(data.CuringAcceptDialogue, EDialogueUiState.CuringAccept);
        }
        else
        {
            _dialogueController.ShowDefaultChoices();
        }
    }

    private void ApplyCuring(PlayerController player)
    {
        if (player == null) return;

        PlayerStaminaAbility staminaAbility = player.GetAbility<PlayerStaminaAbility>();
        staminaAbility?.RecoverFull();

        PlayerHelperInventoryAbility helperInventoryAbility = player.GetAbility<PlayerHelperInventoryAbility>();
        helperInventoryAbility?.RecoverAllHelpersFull();

        PlayerCuringAbility curingAbility = player.GetAbility<PlayerCuringAbility>();
        curingAbility?.MarkCuredToday();
    }

    private void OnSelectDecline(NpcInteractionContext context)
    {
        NpcDataSO data = context.Npc.Data;
        if (data == null || data.CuringDeclineDialogue == null)
        {
            _dialogueController.ShowDefaultChoices();
            return;
        }

        _dialogueController.StartDialogue(data.CuringDeclineDialogue, EDialogueUiState.CuringDecline);
    }

    private bool CanReceiveCuringToday(PlayerController player)
    {
        if (player == null) return false;

        PlayerCuringAbility curingAbility = player.GetAbility<PlayerCuringAbility>();
        if (curingAbility == null) return true;

        return curingAbility.CanReceiveCuringToday();
    }
}
