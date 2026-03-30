using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.IO;

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
        catch (OperationCanceledException)
        {
            Debug.Log("AI dialogue가 취소되었습니다.");
        }
        catch (IOException e)
        {
            Debug.LogError($"IO error: {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            throw; // 숨기지 않고 표시한다.
        }
    }
}
