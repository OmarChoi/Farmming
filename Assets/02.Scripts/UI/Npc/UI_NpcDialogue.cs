using TMPro;
using UnityEngine;

public class UI_NpcDialogue : MonoBehaviour
{
    public static UI_NpcDialogue Instance { get; private set; }

    [SerializeField] private Transform _interactionButtonRoot;
    [SerializeField] private GameObject _interactionButtonPrefab;
    [SerializeField] private TextMeshProUGUI _npcNameText;
    [SerializeField] private TextMeshProUGUI _npcDialogueText;

    private NpcController _currentNpc;
    private NpcInteractionComponent _currentInteractionComponent;
    private InteractService _interactionService;
    private UI_InteractionButton _uiInteractionButton;

    private Transform _currentInteractor;

    private void Awake()
    {
        Instance = this;
        _interactionService = new InteractService();
        gameObject.SetActive(false);
    }

    public void Open(NpcController npc, Transform interactor)
    {
        _currentNpc = npc;
        _currentInteractor = interactor;
        _currentInteractionComponent = npc.GetComponent<NpcInteractionComponent>();

        SetDialogueText(npc);
        RefreshButtons();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);

        _currentNpc = null;
        _currentInteractor = null;
        _currentInteractionComponent = null;
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

        NpcInteractionContext context = new NpcInteractionContext(
            _currentNpc,
            _currentInteractor,
            _currentInteractionComponent);

        _interactionService.Execute(type, context);
    }

    private void SetDialogueText(NpcController npc)
    {
        string name = npc.Data.NpcName;
        int dialogueNumber = Random.Range(0, npc.Data.StartDialogues.Length);
        string dialogue = npc.Data.StartDialogues[dialogueNumber];

        _npcNameText.text = $"{name}";
        _npcDialogueText.text = $"{dialogue}";
    }
}
