using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleSaveSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text _slotText;
    [SerializeField] private Button _enterButton;
    [SerializeField] private Button _deleteButton;

    public void Setup(int slotIndex, bool hasSave, Action onSelect, Action onDelete)
    {
        _slotText.text = hasSave
            ? $"슬롯 {slotIndex + 1} - 저장 데이터 있음"
            : $"슬롯 {slotIndex + 1} - 비어있음";

        _enterButton.onClick.RemoveAllListeners();
        if (onSelect != null)
            _enterButton.onClick.AddListener(() => onSelect.Invoke());

        _deleteButton.gameObject.SetActive(hasSave);
        _deleteButton.onClick.RemoveAllListeners();
        if (hasSave)
            _deleteButton.onClick.AddListener(() => onDelete?.Invoke());
    }
}