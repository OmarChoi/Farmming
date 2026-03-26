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
    [SerializeField] private Button _exitButton;
    [SerializeField] private TextMeshProUGUI _chatText;
    [SerializeField] private TextMeshProUGUI _npcNameText;

    public event Action<string> OnSendRequested;
    public event Action OnStopRequested;
    public event Action OnCloseRequested;

    private void Awake()
    {
        _sendButton.onClick.RemoveAllListeners();
        _sendButton.onClick.AddListener(HandleSendClicked);

        _stopButton.onClick.RemoveAllListeners();
        _stopButton.onClick.AddListener(() => OnStopRequested?.Invoke());

        _exitButton.onClick.RemoveAllListeners();
        _exitButton.onClick.AddListener(() => OnCloseRequested?.Invoke());

        _stopButton.gameObject.SetActive(false);
    }

    public void Open(string npcName)
    {
        gameObject.SetActive(true);
        if (_npcNameText != null)
        {
            _npcNameText.text = npcName;
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void ClearMessages()
    {
        if (_chatText != null)
        {
            _chatText.text = string.Empty;
        }
    }

    public void AddSystemMessage(string message)
    {
        if (_chatText == null) return;
        _chatText.text += $"[시스템] {message}\n";
    }

    public void AddPlayerMessage(string message)
    {
        if (_chatText == null) return;
        _chatText.text += $"플레이어: {message}\n";
    }

    public void BeginNpcStreaming()
    {
        if (_chatText == null) return;
        _chatText.text += "NPC: ...\n";
    }

    public void UpdateNpcStreaming(string partial)
    {
        if (_chatText == null) return;

        string[] lines = _chatText.text.Split('\n');
        if (lines.Length == 0) return;

        int lastIndex = lines.Length - 1;
        if (lastIndex > 0 && string.IsNullOrEmpty(lines[lastIndex]))
        {
            lastIndex--;
        }

        if (lastIndex < 0) return;

        lines[lastIndex] = $"NPC: {partial}";
        _chatText.text = string.Join("\n", lines);
    }

    public void CompleteNpcStreaming(string reply)
    {
        UpdateNpcStreaming(reply);

        if (_chatText != null && !_chatText.text.EndsWith("\n"))
        {
            _chatText.text += "\n";
        }
    }

    public void MarkStreamingStopped()
    {
        if (_chatText == null) return;
        _chatText.text += "[응답 중단]\n";
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
        if (_exitButton != null) _exitButton.interactable = true;
        if (_inputField != null) _inputField.interactable = !isGenerating;
    }

    private void HandleSendClicked()
    {
        if (_inputField == null) return;
        OnSendRequested?.Invoke(_inputField.text);
    }
}
