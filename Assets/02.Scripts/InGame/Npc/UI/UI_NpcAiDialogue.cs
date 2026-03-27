using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class UI_NpcAiDialogue : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Button _sendButton;
    [SerializeField] private Button _stopButton;
    [SerializeField] private TextMeshProUGUI _playerChatText;
    [SerializeField] private TextMeshProUGUI _npcChatText;
    [SerializeField] private TextMeshProUGUI _systemChatText;
    [SerializeField] private TextMeshProUGUI _npcNameText;

    [SerializeField] private GameObject _playerAiChatPrefab;

    public event Action<string> OnSendRequested;
    public event Action OnStopRequested;

    private void Awake()
    {
        _sendButton.onClick.RemoveAllListeners();
        _sendButton.onClick.AddListener(HandleSendClicked);

        _stopButton.onClick.RemoveAllListeners();
        _stopButton.onClick.AddListener(() => OnStopRequested?.Invoke());

        _stopButton.gameObject.SetActive(false);
    }

    public void Open(string npcName)
    {
        _playerAiChatPrefab.SetActive(true);
        if (_npcNameText != null)
        {
            _npcNameText.text = npcName;
        }
    }

    public void Close()
    {
        _playerAiChatPrefab.SetActive(false);
    }

    public void ClearMessages()
    {
        if (_playerChatText != null)
        {
            _playerChatText.text = string.Empty;
        }
        if (_npcChatText != null)
        {
            _npcChatText.text = string.Empty;
        }
        if (_systemChatText != null)
        {
            _systemChatText.text = string.Empty;
        }
    }

    public void AddSystemMessage(string message)
    {
        if (_systemChatText == null) return;
        _systemChatText.text += $"{message}\n";
    }

    public void AddPlayerMessage(string message)
    {
        if (_playerChatText == null) return;
        _playerChatText.text += $"{message}\n";
    }

    public void BeginNpcStreaming()
    {
        if (_npcChatText == null) return;
        _npcChatText.text += "...\n";
    }

    public void UpdateNpcStreaming(string partial)
    {
        if (_npcChatText == null) return;

        string[] lines = _npcChatText.text.Split('\n');
        if (lines.Length == 0) return;

        int lastIndex = lines.Length - 1;
        if (lastIndex > 0 && string.IsNullOrEmpty(lines[lastIndex]))
        {
            lastIndex--;
        }

        if (lastIndex < 0) return;

        lines[lastIndex] = $"NPC: {partial}";
        _npcChatText.text = string.Join("\n", lines);
    }

    public void CompleteNpcStreaming(string reply)
    {
        UpdateNpcStreaming(reply);

        if (_npcChatText != null && !_npcChatText.text.EndsWith("\n"))
        {
            _npcChatText.text += "\n";
        }
    }

    public void MarkStreamingStopped()
    {
        if (_npcChatText == null) return;
        _npcChatText.text += "\n";
    }

    public void ClearInputField()
    {
        if (_inputField != null)
        {
            _inputField.text = string.Empty;
        }
    }

    public void SetGenerating(bool isGenerating)
    {
        if (_sendButton != null) _sendButton.gameObject.SetActive(!isGenerating);
        if (_stopButton != null) _stopButton.gameObject.SetActive(isGenerating);
        if (_inputField != null) _inputField.interactable = !isGenerating;
    }

    private void HandleSendClicked()
    {
        if (_inputField == null) return;
        OnSendRequested?.Invoke(_inputField.text);
    }
}
