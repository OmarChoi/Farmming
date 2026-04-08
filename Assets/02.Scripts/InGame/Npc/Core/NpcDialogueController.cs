using UnityEngine;
using System;
using System.Collections.Generic;

public class NpcDialogueController : MonoBehaviour
{
    [SerializeField] private NpcFriendshipManager _friendshipManager;
    [SerializeField] private UI_NpcDialogue _uiDialogue;
    [SerializeField] private UI_FriendshipBar _uiFriendshipBar;
    [SerializeField] private InteractService _interactionService;

    private IFriendshipService _friendshipService;

    private NpcController _currentNpc;
    private Transform _currentInteractor;
    private NpcInteractionComponent _currentInteractionComponent;
    private NpcAnimatorController _anim;

    private NpcDialogueSO _currentDialogue;
    private int _currentLineIndex;

    private EDialogueUiState _dialogueState = EDialogueUiState.None;

    private Action _onDialogueEnded;

    private void Awake()
    {
        if (_friendshipManager == null)
        {
            _friendshipManager = FindFirstObjectByType<NpcFriendshipManager>();
        }
        _friendshipService = _friendshipManager;

        if (_uiDialogue == null)
        {
            _uiDialogue = FindFirstObjectByType<UI_NpcDialogue>();
        }
        if (_uiFriendshipBar == null)
        {
            _uiFriendshipBar = FindFirstObjectByType<UI_FriendshipBar>();
        }
        if (_interactionService == null)
        {
            _interactionService = FindFirstObjectByType<InteractService>();
        }
    }
    private void OnEnable()
    {
        if (_friendshipService != null)
        {
            _friendshipService.OnFriendshipChanged += HandleFriendshipChanged;
        }
    }

    private void OnDisable()
    {
        if (_friendshipService != null)
        {
            _friendshipService.OnFriendshipChanged -= HandleFriendshipChanged;
        }
    }

    private void Start()
    {
        _uiDialogue.BindDialoguePanel(OnClickDialoguePanel);
    }

    public void Open(NpcController npc, Transform interactor)
    {
        _currentNpc = npc;
        _currentInteractor = interactor;
        _currentInteractionComponent = npc.GetComponent<NpcInteractionComponent>();
        _anim = npc.Anim;

        _uiDialogue.Open();
        _uiDialogue.SetNpcNameText(npc.Data.NpcName);
        _uiDialogue.HideButtons();
        _uiDialogue.ClearDialogueText();

        if (_uiFriendshipBar != null)
        {
            _uiFriendshipBar.BindNpc(npc.Data.NpcId, true);
        }

        QuestManager.Instance?.ReportNpcTalked(npc.Data.NpcId);

        if (npc.Data != null && npc.Data.AutoStartQuestOnInteract)
        {
            _dialogueState = EDialogueUiState.Quest;
            _interactionService.Execute(ENpcInteractionType.Quest, CreateContext());
            return;
        }

        StartGreeting();
    }

    public void Close()
    {
        _currentNpc = null;
        _currentInteractor = null;
        _currentInteractionComponent = null;
        _currentDialogue = null;
        _currentLineIndex = 0;
        _dialogueState = EDialogueUiState.None;

        if (_uiFriendshipBar != null)
        {
            _uiFriendshipBar.Clear();
        }

        _uiDialogue.Close();
    }

    private void HandleFriendshipChanged(string npcId, int oldValue, int newValue, ENpcFriendshipReason reason)
    {
        if (_currentNpc?.Data?.NpcId != npcId || _uiFriendshipBar == null)
        {
            return;
        }

        _uiFriendshipBar.RefreshAnimated(newValue);
    }

    public void PrepareForDeepTalk()
    {
        _currentDialogue = null;
        _currentLineIndex = 0;
        _dialogueState = EDialogueUiState.None;

        _uiDialogue.HideButtons();
        _uiDialogue.ClearDialogueText();
    }

    private void StartGreeting()
    {
        if (_currentNpc == null || _interactionService == null) return;

        _dialogueState = EDialogueUiState.Greeting;
        _interactionService.StartGreeting(CreateContext());
    }

    public void StartDialogue(NpcDialogueSO dialogueSO, EDialogueUiState dialogueState)
    {
        _dialogueState = dialogueState;
        StartDialogue(dialogueSO);
    }

    public void StartDialogue(NpcDialogueSO dialogueSO, EDialogueUiState dialogueState, Action onEnded)
    {
        _dialogueState = dialogueState;
        _onDialogueEnded = onEnded;
        StartDialogue(dialogueSO);
    }

    public void StartDialogue(NpcDialogueSO dialogueSO)
    {
        if (dialogueSO == null || dialogueSO.Lines == null || dialogueSO.Lines.Length == 0)
        {
            EndDialogue();
            return;
        }

        _currentDialogue = dialogueSO;
        _currentLineIndex = 0;

        _uiDialogue.Open();
        _uiDialogue.HideButtons();

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

        _uiDialogue.ShowLine(_currentDialogue.Lines[_currentLineIndex].Text);
    }

    private void NextLine()
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
                StartDialogue(lastLine.NextDialogue);
                return;
            }
        }

        _currentDialogue = null;
        _currentLineIndex = 0;

        Action endedCallback = _onDialogueEnded;
        _onDialogueEnded = null;
        if (endedCallback != null)
        {
            endedCallback.Invoke();
            return;
        }

        switch (_dialogueState)
        {
            case EDialogueUiState.Greeting:
                _dialogueState = EDialogueUiState.Choice;
                ShowChoiceButtons(_currentNpc.InteractionOptions);
                break;

            case EDialogueUiState.Talking:
            case EDialogueUiState.Quest:
                _dialogueState = EDialogueUiState.None;
                EndCurrentInteraction();
                break;

            default:
                _dialogueState = EDialogueUiState.None;
                break;
        }
    }
    private void ShowChoiceButtons(NpcInteractionOption[] interactionOptions)
    {
        if (_currentNpc == null) return;
        _uiDialogue.ShowButtons(interactionOptions, OnClickOption);
    }

    public void ShowDefaultChoices()
    {
        if (_currentNpc == null || _uiDialogue == null) return;

        _currentDialogue = null;
        _currentLineIndex = 0;
        _dialogueState = EDialogueUiState.Choice;

        _uiDialogue.ClearDialogueText();
        ShowChoiceButtons(_currentNpc.InteractionOptions);
    }

    public void ShowQuestChoices(IReadOnlyList<NpcDialogueChoiceData> choices, bool clearText = true)
    {
        if (_uiDialogue == null) return;

        if (clearText)
        {
            _uiDialogue.ClearDialogueText();
        }

        _uiDialogue.ShowChoiceButtons(choices);
    }

    private void OnClickOption(ENpcInteractionType type)
    {
        if (_currentNpc == null || _interactionService == null) return;

        switch (type)
        {
            case ENpcInteractionType.Talk:
                _anim?.PlayTalk();
                _dialogueState = EDialogueUiState.Talking;
                _interactionService.Execute(type, CreateContext());
                break;

            case ENpcInteractionType.DeepTalk:
            case ENpcInteractionType.Trade:
            case ENpcInteractionType.Upgrade:
            case ENpcInteractionType.Styling:
            case ENpcInteractionType.Quest:
            case ENpcInteractionType.EndTalk:
                _interactionService.Execute(type, CreateContext());
                break;
        }
    }

    // 클릭으로 다음 대화문으로 넘어가는 메서드입니다.
    public void OnClickDialoguePanel()
    {
        if (_currentDialogue == null) return;

        if (_uiDialogue.IsTyping())
        {
            // 타이핑 도중이면 글씨 표시를 즉시 완료한다.
            _uiDialogue.CompleteTyping();
        }
        else
        {
            // 다 끝났으면 다음 줄로 넘어간다.
            NextLine();
        }
    }

    private void EndCurrentInteraction()
    {
        if (_interactionService == null) return;
        _interactionService.Execute(ENpcInteractionType.EndTalk, CreateContext());
    }

    private NpcInteractionContext CreateContext()
    {
        return new NpcInteractionContext(
            _currentNpc,
            _currentInteractor,
            _currentInteractionComponent);
    }
}
