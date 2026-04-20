using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class UI_NpcDialogue : MonoBehaviour
{
    public static UI_NpcDialogue Instance { get; private set; }

    [Header("컴포넌트 참조")]
    [SerializeField] private Transform _interactionButtonRoot;
    [SerializeField] private RectTransform _interactionButtonRect;
    [SerializeField] private GameObject _interactionButtonPrefab;
    [SerializeField] private TextMeshProUGUI _npcNameText;
    [SerializeField] private TextMeshProUGUI _npcDialogueText;
    [SerializeField] private Button _dialoguePanelButton;
    [SerializeField] private TypewriterWithWrap _typewriter;

    public bool IsReady => _dialoguePanelButton != null && _typewriter != null;

    private Action _onClickDialoguePanel;
    private NpcVoicePlayer _activeVoice;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    { 
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (_dialoguePanelButton != null)
        {
            _dialoguePanelButton.onClick.AddListener(() =>
            {
                _onClickDialoguePanel?.Invoke();
            });
        }
    }

    public void BindDialoguePanel(Action onClick)
    {
        _onClickDialoguePanel = onClick;
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        StopActiveVoice();
        gameObject.SetActive(false);
        ClearButtons();
        ClearDialogueText();
    }

    public void SetNpcNameText(string npcName)
    {
        _npcNameText.text = npcName;
    }

    public void ShowLine(string text)
    {
        ShowLine(text, null);
    }

    public void ShowLine(string text, NpcVoicePlayer voice)
    {
        gameObject.SetActive(true);
        StopActiveVoice();
        _activeVoice = voice;

        if (voice != null)
        {
            voice.ResetRepeatFilter();
            _typewriter.StartTyping(text, voice.PlayChar);
        }
        else
        {
            _typewriter.StartTyping(text);
        }
    }

    public void CompleteTyping()
    {
        _typewriter.CompleteTyping();
        StopActiveVoice();
    }

    private void StopActiveVoice()
    {
        if (_activeVoice != null)
        {
            _activeVoice.Stop();
            _activeVoice = null;
        }
    }

    public bool IsTyping()
    {
        return _typewriter.IsTyping();
    }

    // NPC가 가진 상호작용에 관한 버튼을 생성하는 메서드입니다.
    public void ShowButtons(NpcInteractionOption[] options, Action<ENpcInteractionType> onClickOption)
    {
        if (options == null)
        {
            HideButtons();
            return;
        }

        var choices = new List<NpcDialogueChoiceData>();

        foreach (var option in options)
        {
            var capturedType = option.Type;
            choices.Add(new NpcDialogueChoiceData(option.ButtonName, () => onClickOption?.Invoke(capturedType)));
        }

        ShowChoiceButtons(choices);
    }

    // 버튼을 생성해주는 범용 메서드입니다.
    public void ShowChoiceButtons(IReadOnlyList<NpcDialogueChoiceData> choices)
    {
        ClearButtons();
        _interactionButtonRoot.gameObject.SetActive(true);

        if (choices == null || choices.Count == 0) return;

        foreach (NpcDialogueChoiceData choice in choices)
        {
            if (choice == null) continue;

            GameObject button = Instantiate(_interactionButtonPrefab, _interactionButtonRoot);
            UI_InteractionButton uiButton = button.GetComponent<UI_InteractionButton>();
            uiButton.Init(choice.ButtonText, choice.OnClick);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(_interactionButtonRect);
    }

    public void HideButtons()
    {
        ClearButtons();
        _interactionButtonRoot.gameObject.SetActive(false);
    }

    private void ClearButtons()
    {
        for (int i = _interactionButtonRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(_interactionButtonRoot.GetChild(i).gameObject);
        }
    }

    public void ClearDialogueText()
    {
        _npcDialogueText.text = string.Empty;
    }
}
