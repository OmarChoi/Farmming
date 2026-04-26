using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_NpcAiDialogue : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Button _sendButton;
    [SerializeField] private Button _stopButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TextMeshProUGUI _playerChatText;
    [SerializeField] private TextMeshProUGUI _npcChatText;
    [SerializeField] private TextMeshProUGUI _systemChatText;
    [SerializeField] private TextMeshProUGUI _npcNameText;

    [Header("컴포넌트 참조")]
    [SerializeField] private TypewriterWithWrap _wrapper;

    [Header("PlayerAiChat 루트")]
    [SerializeField] private GameObject _playerAiChatRoot;

    private string _currentNpcStreamingRaw = string.Empty;
    private NpcVoicePlayer _voice;

    public event Action<string> OnSendRequested;
    public event Action OnStopRequested;
    public event Action OnCloseRequested;

    private void Awake()
    {
        if (_wrapper == null)
        {
            _wrapper = FindFirstObjectByType<TypewriterWithWrap>();
        }
        _sendButton.onClick.RemoveAllListeners();
        _sendButton.onClick.AddListener(HandleSendClicked);

        _stopButton.onClick.RemoveAllListeners();
        _stopButton.onClick.AddListener(() => OnStopRequested?.Invoke());

        _closeButton.onClick.RemoveAllListeners();
        _closeButton.onClick.AddListener(() => OnCloseRequested?.Invoke());

        _stopButton.gameObject.SetActive(false);
        _closeButton.gameObject.SetActive(false);
        _playerAiChatRoot.SetActive(false);
    }

    public void Open(string npcName)
    {
        _playerAiChatRoot.SetActive(true);
        _closeButton.gameObject.SetActive(true);
        if (_npcNameText != null)
        {
            _npcNameText.text = npcName;
        }
    }

    public void Close()
    {
        _voice?.Stop();
        _closeButton.gameObject.SetActive(false);
        _playerAiChatRoot.SetActive(false);
    }

    public void SetVoice(NpcVoicePlayer voice)
    {
        if (_voice != null && _voice != voice)
        {
            _voice.Stop();
        }
        _voice = voice;
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

    public void ClearMessagesExcludePlayer()
    {
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
        _currentNpcStreamingRaw = string.Empty;
        _npcChatText.text = "...\n";
        _voice?.BeginStreaming();
    }

    public void UpdateNpcStreaming(string partial)
    {
        if (_npcChatText == null) return;

        _currentNpcStreamingRaw = partial ?? string.Empty;

        if (string.IsNullOrEmpty(_currentNpcStreamingRaw))
        {
            _npcChatText.text = "...\n";
            return;
        }

        _npcChatText.text = _wrapper.WrapText(_currentNpcStreamingRaw, _npcChatText);
        _voice?.PlayStreaming(_currentNpcStreamingRaw);
    }

    public void CompleteNpcStreaming(string reply)
    {
        if (_npcChatText == null || _wrapper == null) return;

        // 원본 데이터를 기준으로 합니다.
        _currentNpcStreamingRaw = reply ?? string.Empty;

        // 줄 바꿈을 적용해서 화면에 보여줍니다.
        _npcChatText.text = _wrapper.WrapText(_currentNpcStreamingRaw, _npcChatText);

        if (!_npcChatText.text.EndsWith("\n"))
        {
            _npcChatText.text += "\n";
        }

        // 잔여 큐는 그대로 드레인되도록 두고, 여기선 중지하지 않는다.
        // 대화창이 닫히거나 다음 발화가 시작되면 Stop이 호출된다.
    }

    public void SetNpcStatusMessage(string message)
    {
        if (_npcChatText == null) return;

        _currentNpcStreamingRaw = message ?? string.Empty;

        if (_wrapper != null)
        {
            _npcChatText.text = _wrapper.WrapText(_currentNpcStreamingRaw, _npcChatText);
        }
        else
        {
            _npcChatText.text = _currentNpcStreamingRaw;
        }

        if (!_npcChatText.text.EndsWith("\n"))
        {
            _npcChatText.text += "\n";
        }
    }

    public void MarkStreamingStopped()
    {
        if (_npcChatText == null) return;

        if (!_npcChatText.text.EndsWith("\n"))
        {
            _npcChatText.text += "\n";
        }
        _voice?.Stop();
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
