using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleSaveSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text _slotText;
    [SerializeField] private Button _button;

    public void Setup(int slotIndex, bool hasSave, Action onSelect)
    {
        _slotText.text = hasSave
            ? $"슬롯 {slotIndex + 1} - 저장 데이터 있음"
            : $"슬롯 {slotIndex + 1} - 비어있음";

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onSelect?.Invoke());
    }
}