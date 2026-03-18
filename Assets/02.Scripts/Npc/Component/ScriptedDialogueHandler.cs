using UnityEngine;

public class ScriptedDialogueHandler : IDialogueHandler
{
    private readonly UI_NpcDialogue _uiDialogue;

    public ScriptedDialogueHandler(UI_NpcDialogue uiDialogue)
    {
        _uiDialogue = uiDialogue;
    }

    public void StartDialogue(NpcInteractionContext context, bool isStart)
    {
        NpcDialogueSO[] dialogueArray = isStart
            ? context.Npc.Data.StartDialogues
            : context.Npc.Data.TalkDialogues;

        if (dialogueArray == null || dialogueArray.Length == 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[{context.NpcName}] 대사 데이터가 비어 있습니다. isStart = {isStart}");
#endif
            return;
        }

        int randomIndex = Random.Range(0, dialogueArray.Length);
        NpcDialogueSO selectedDialogue = dialogueArray[randomIndex];

        if (selectedDialogue == null || selectedDialogue.Lines == null || selectedDialogue.Lines.Length == 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[{context.NpcName}] 선택된 대사 SO가 비어 있습니다.");
#endif
            return;
        }

        _uiDialogue.StartDialogueUi(selectedDialogue);
    }
}
