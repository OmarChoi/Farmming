using System;
using UnityEngine;

public class UI_DinoRace : MonoBehaviour
{
    [SerializeField] private UI_DinoRaceSelectionPanel _selectionPanel;

    private void Awake()
    {
        Close();
    }

    public void OpenSelection(
        string[] runnerNames,
        int currentGold,
        int defaultBetAmount,
        int betStepAmount,
        int minBetAmount,
        Action<int, int> onConfirm,
        Action onClose)
    {
        gameObject.SetActive(true);
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

    public void Close()
    {
        _selectionPanel?.Hide();
        gameObject.SetActive(false);
    }
}
