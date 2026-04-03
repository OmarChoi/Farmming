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
    [SerializeField] private GameObject _interactionButtonPrefab;
    [SerializeField] private TextMeshProUGUI _npcNameText;
    [SerializeField] private TextMeshProUGUI _npcDialogueText;
    [SerializeField] private Button _dialoguePanelButton;
    [SerializeField] private TypewriterWithWrap _typewriter;

    private Action _onClickDialoguePanel;

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
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
        gameObject.SetActive(true);
        _typewriter.StartTyping(text);
    }

    public bool IsTyping()
    {
        return _typewriter.IsTyping();
    }

    public void CompleteTyping()
    {
        _typewriter.CompleteTyping();
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
