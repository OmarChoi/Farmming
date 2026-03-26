using UnityEngine;
using Cysharp.Threading.Tasks;

public class AIDialogueHandler : IDialogueHandler
{
    private readonly AiDialogueController _aiDialogueController;

    public AIDialogueHandler(AiDialogueController aiDialogueController)
    {
        _aiDialogueController = aiDialogueController;
    }

    public void StartDialogue(NpcInteractionContext context, bool isStart)
    {
        if (_aiDialogueController == null || context == null) return;

        OpenAiDialogueAsync(context).Forget();
    }

    private async UniTask OpenAiDialogueAsync(NpcInteractionContext context)
    {
        try
        {
            await _aiDialogueController.OpenSessionAsync(context);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
    }
}
