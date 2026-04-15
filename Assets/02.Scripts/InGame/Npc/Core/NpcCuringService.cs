using UnityEngine;
using System.Collections.Generic;

public class NpcCuringService : MonoBehaviour
{
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

        // 나중에 실제 플레이어 정보를 받아와서 오늘 이미 치료 받았는지 판정하도록 수정해야 합니다.
        bool alreadyCuredToday = !CanReceiveCuringToday(context.Interactor);

        if (alreadyCuredToday)
        {
            if(data.CuringAlreadyDoneDialogue != null)
            {
                _dialogueController.StartDialogue(data.CuringAlreadyDoneDialogue, EDialogueUiState.CuringAlreadyDone);
            }
            else if(data.CuringDeclineDialogue != null)
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

        _dialogueController.StartDialogue(data.CuringAcceptDialogue, EDialogueUiState.CuringAccept);
    }

    private void ApplyCuring(PlayerController player)
    {
        if (player == null) return;

        PlayerStaminaAbility staminaAbility = player.GetAbility<PlayerStaminaAbility>();
        staminaAbility?.RecoverFull();

        PlayerHelperInventoryAbility helperInventoryAbility = player.GetAbility<PlayerHelperInventoryAbility>();
        helperInventoryAbility?.RecoverAllHelpersFull();
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
        // todo. 오늘 이미 치료 받았는지 판정합니다.
        return true;
    }
}
