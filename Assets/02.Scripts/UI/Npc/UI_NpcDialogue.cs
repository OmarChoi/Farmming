using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_NpcDialogue : MonoBehaviour
{
    public static UI_NpcDialogue Instance { get; private set; }

    [Header("컴포넌트 참조")]
    [SerializeField] private Transform _interactionButtonRoot;
    [SerializeField] private GameObject _interactionButtonPrefab;
    [SerializeField] private TextMeshProUGUI _npcNameText;
    [SerializeField] private TextMeshProUGUI _npcDialogueText;
    [SerializeField] private Button _dialoguePanelButton;
    [SerializeField] private InteractService _interactionService;

    private NpcController _currentNpc;
    private NpcInteractionComponent _currentInteractionComponent;
    private UI_InteractionButton _uiInteractionButton;

    private Transform _currentInteractor;

    private NpcDialogueSO _currentDialogue;
    private int _currentLineIndex;

    private EDialogueUiState _dialogueState = EDialogueUiState.None;

    private void Awake()
    {
        Instance = this;
        if (_interactionService == null)
        {
            _interactionService = FindFirstObjectByType<InteractService>();
        }
        gameObject.SetActive(false);
    }

    public void Open(NpcController npc, Transform interactor)
    {
        _currentNpc = npc;
        _currentInteractor = interactor;
        _currentInteractionComponent = npc.GetComponent<NpcInteractionComponent>();

        SetNpcNameText(npc);
        gameObject.SetActive(true);

        HideButtons();
        ClearDialogueText();

        StartGreeting();
    }

    public void Close()
    {
        gameObject.SetActive(false);

        _currentNpc = null;
        _currentInteractor = null;
        _currentInteractionComponent = null;
        _currentDialogue = null;
        _currentLineIndex = 0;
        _dialogueState = EDialogueUiState.None;

        ClearButtons();
        ClearDialogueText();
    }

    public void SetActiveFalse()
    {
        gameObject.SetActive(false);
    }

    private void SetNpcNameText(NpcController npc)
    {
        string name = npc.Data.NpcName;
        _npcNameText.text = $"{name}";
    }

    private void StartGreeting()
    {
        if (_currentNpc == null) return;
        if (_interactionService == null) return;

        _dialogueState = EDialogueUiState.Greeting;

        NpcInteractionContext context = new NpcInteractionContext(
            _currentNpc,
            _currentInteractor,
            _currentInteractionComponent);

        _interactionService.ExecuteStartTalk(context);
    }

    public void StartDialogueUi(NpcDialogueSO dialogueSO)
    {
        if (dialogueSO == null || dialogueSO.Lines == null || dialogueSO.Lines.Length == 0)
        {
            EndDialogue();
            return;
        }

        _currentDialogue = dialogueSO;
        _currentLineIndex = 0;

        HideButtons();
        gameObject.SetActive(true);

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (_currentDialogue == null || _currentDialogue.Lines == null)
        {
            EndDialogue();
            return;
        }

        if (_currentLineIndex >= _currentDialogue.Lines.Length)
        {
            EndDialogue();
            return;
        }

        _npcDialogueText.text = _currentDialogue.Lines[_currentLineIndex].Text;
    }

    public void NextLine()
    {
        if (_currentDialogue == null) return;

        _currentLineIndex++;
        ShowCurrentLine();
    }

    private void EndDialogue()
    {
        if (_currentDialogue != null && _currentDialogue.Lines != null && _currentDialogue.Lines.Length > 0)
        {
            var lastLine = _currentDialogue.Lines[_currentDialogue.Lines.Length - 1];

            if (lastLine.NextDialogue != null)
            {
                StartDialogueUi(lastLine.NextDialogue);
                return;
            }
        }

        _currentDialogue = null;
        _currentLineIndex = 0;

        switch (_dialogueState)
        {
            case EDialogueUiState.Greeting:
                _dialogueState = EDialogueUiState.Choice;
                ShowButtons();
                break;

            case EDialogueUiState.Talking:
                _dialogueState = EDialogueUiState.None;
                EndCurrentInteraction();
                break;

            default:
                _dialogueState = EDialogueUiState.None;
                break;
        }
    }

    private void EndCurrentInteraction()
    {
        if (_currentNpc == null) return;
        if (_interactionService == null) return;

        NpcInteractionContext context = new NpcInteractionContext(
            _currentNpc,
            _currentInteractor,
            _currentInteractionComponent);

        _interactionService.Execute(ENpcInteractionType.EndTalk, context);
    }

    private void ShowButtons()
    {
        _interactionButtonRoot.gameObject.SetActive(true);
        RefreshButtons();
    }

    private void HideButtons()
    {
        ClearButtons();
        _interactionButtonRoot.gameObject.SetActive(false);
    }

    private void RefreshButtons()
    {
        ClearButtons();

        if (_currentNpc == null || _currentNpc.InteractionOptions == null) return;

        foreach (var option in _currentNpc.InteractionOptions)
        {
            GameObject button = Instantiate(_interactionButtonPrefab, _interactionButtonRoot);
            UI_InteractionButton uiButton = button.GetComponent<UI_InteractionButton>();
            uiButton.Init(option.ButtonName, () => OnClickOption(option.Type));
        }
    }

    private void ClearButtons()
    {
        for (int i = _interactionButtonRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(_interactionButtonRoot.GetChild(i).gameObject);
        }
    }

    private void OnClickOption(ENpcInteractionType type)
    {
        if (_currentNpc == null) return;
        if (_interactionService == null) return;

        NpcInteractionContext context = new NpcInteractionContext(
            _currentNpc,
            _currentInteractor,
            _currentInteractionComponent);

        switch (type)
        {
            case ENpcInteractionType.Talk:
                _dialogueState = EDialogueUiState.Talking;
                _interactionService.ExecuteNormalTalk(context);
                break;

            case ENpcInteractionType.Trade:
                _interactionService.Execute(type, context);
                break;

            case ENpcInteractionType.EndTalk:
                _interactionService.Execute(type, context);
                break;
        }
    }

    private void ClearDialogueText()
    {
        _npcDialogueText.text = string.Empty;
    }

    // 클릭으로 다음 대화문으로 넘어가는 메서드입니다.
    public void OnClickDialoguePanel()
    {
        if (_currentDialogue == null) return;

        NextLine();
    }
}
