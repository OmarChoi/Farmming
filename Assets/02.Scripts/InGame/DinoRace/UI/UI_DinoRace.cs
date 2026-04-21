using System;
using UnityEngine;

public class UI_DinoRace : UIBase
{
    [SerializeField] private UI_DinoRaceSelectionPanel _selectionPanel;

    public void OpenSelection(
        string[] runnerNames,
        int currentGold,
        int defaultBetAmount,
        int betStepAmount,
        int minBetAmount,
        Action<int, int> onConfirm,
        Action onClose)
    {
        _selectionPanel?.Show(
            runnerNames,
            currentGold,
            defaultBetAmount,
            betStepAmount,
            minBetAmount,
            onConfirm,
            onClose);
    }

    public void SetSelectionMessage(string message)
    {
        _selectionPanel?.SetMessage(message);
    }

    protected override void OnClose()
    {
        _selectionPanel?.Hide();
    }
}
