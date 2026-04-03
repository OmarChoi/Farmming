using System;

public class NpcDialogueChoiceData
{
    public string ButtonText;
    public Action OnClick;

    public NpcDialogueChoiceData(string buttonText, Action onClick)
    {
        ButtonText = buttonText;
        OnClick = onClick;
    }
}
