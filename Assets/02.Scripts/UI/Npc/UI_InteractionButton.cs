using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class UI_InteractionButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _interactionText;

    public void Init(string buttonText, Action onClick)
    {
        _interactionText.text = buttonText;

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onClick?.Invoke());
    }
}
