using Cysharp.Threading.Tasks;
using UnityEngine;

public class ShrineBuilding : DefaultBuilding, IWorldInteractable
{
    [Header("참조 컴포넌트")]
    [SerializeField] private QuestMarkService _questMarkService;

    private bool _closeUi;
    private UI_Shrine _ui;
    private PlayerController _playerController;
    private PlayerNPCInteractionAbility _playerInteraction;

    public bool CanInteract => IsConstructionComplete;

    private void Awake()
    {
        if (_questMarkService == null)
        {
            _questMarkService = FindFirstObjectByType<QuestMarkService>();
        }
    }

    public void Interact(PlayerController player)
    {
        if (player == null) return;

        _questMarkService?.MarkShrineChecked();

        _playerController = player;
        _playerInteraction = player.GetAbility<PlayerNPCInteractionAbility>();
        _playerController.EnterUIMode();

        OpenShrineUiAsync().Forget();
    }
    
    // 완료 버튼에서 호출. 실제 권한 검사는 WorldEffectQuestService가 마스터에서 처리합니다.
    public void RequestCompletion()
    {
        if (!CanInteract) return;

        var service = WorldEffectQuestService.Instance;
        if (service == null || !service.HasActiveQuest) return;

        service.RequestCompleteAtShrine(service.ActiveQuestId);
    }

    private async UniTaskVoid OpenShrineUiAsync()
    {
        if (UIController.Instance == null)
        {
            Debug.LogError($"{nameof(ShrineBuilding)} could not open Shrine UI because {nameof(UIController)} is missing.", this);
            _closeUi = false;
            EndInteract();
            return;
        }

        UI_Shrine ui = await UIController.Instance.OpenAsync(new UILifecycleActions<UI_Shrine>
        {
            OnOpen = instance =>
            {
                instance.Configure(this, EndInteractionFromUI);
            },
        });
        
        if (ui == null)
        {
            _closeUi = false;
            EndInteract();
        }
        _closeUi = true;
    }

    private void EndInteractionFromUI()
    {
        _closeUi = false;
        EndInteract();
    }

    public void EndInteract()
    {
        if (_closeUi)
        {
            _ui?.CloseFromOwner();
        }

        _playerController?.ExitUIMode();
        _playerInteraction?.EndInteraction();
        _playerController = null;
        _playerInteraction = null;
        _ui = null;
    }

    protected override void OnDestroy()
    {
        _closeUi = true;
        EndInteract();
        base.OnDestroy();
    }

    public string AnimationTrigger { get; }
}
