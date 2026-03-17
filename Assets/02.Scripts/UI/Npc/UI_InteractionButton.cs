using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class UI_InteractionButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _interactionText;

    [SerializeField] private Image _background;
    [SerializeField] private Image _place;

    public void Init(string buttonText, Action onClick)
    {
        _interactionText.text = buttonText;
        _button.enabled = true;

        _background.enabled = true;
        _place.enabled = true;

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onClick?.Invoke());
    }
}
