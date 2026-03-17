using UnityEngine;

public class UI_NpcDialogue : MonoBehaviour
{
    public static UI_NpcDialogue Instance { get; private set; }

    [SerializeField] private Transform _interactionButtonRoot;
    [SerializeField] private UI_InteractionButton _interactionButtonPrefab;

    private NpcController _currentNpc;
    private Transform _currentInteractor;
    private NpcInteractionComponent _currentInteractionComponent;
    private InteractService _interactionService;

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

        RefreshButtons();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        ClearButtons();
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
            UI_InteractionButton button = Instantiate(_interactionButtonPrefab, _interactionButtonRoot);
            button.Init(option.ButtonName, () => OnClickOption(option.Type));
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
}
