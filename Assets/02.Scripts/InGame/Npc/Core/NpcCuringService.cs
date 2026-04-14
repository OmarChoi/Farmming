using System.Collections.Generic;
using UnityEngine;

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
        bool alreadyCuredToday = false;

        if (alreadyCuredToday)
        {
            if (data.CuringAlreadyDoneDialogue != null)
            {
                _dialogueController.StartDialogue(data.CuringAlreadyDoneDialogue, EDialogueUiState.CuringDecline);
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

        // todo.여기서 치료 처리를 호출합니다.

        _dialogueController.StartDialogue(data.CuringAcceptDialogue, EDialogueUiState.CuringAccept);
    }

    private void OnSelectDecline(NpcInteractionContext context)
    {
        NpcDataSO data = context.Npc.Data;
        if (data == null || data.CuringDeclineDialogue == null)
        {
            _dialogueController.ShowDefaultChoices();
            return;
        }

        _dialogueController.StartDialogue( data.CuringDeclineDialogue, EDialogueUiState.CuringDecline);
    }

    private bool CanReceiveCuringToday(PlayerController player)
    {
        // todo. 오늘 이미 치료 받았는지 판정합니다.
        return true;
    }
}
