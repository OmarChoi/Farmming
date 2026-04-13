using Cysharp.Threading.Tasks;
using UnityEngine;

public class ShrineBuilding : DefaultBuilding, IInteraction
{
    [Header("Test Quest")]
    [SerializeField] private string _questTitle = "제단 의뢰";
    [TextArea(3, 6)]
    [SerializeField] private string _questDescription = "임시 제단 의뢰입니다. 완료 요청 후 성공/실패를 선택하면 월드 버프/디버프가 적용됩니다.";
    [TextArea(2, 4)]
    [SerializeField] private string _completionRequestText = "의뢰 완료 결과를 선택해 주세요.";

    [Header("Test World Effects")]
    [SerializeField] private string _successEffectId = "Shrine_Test_Blessing";
    [SerializeField] private string _failureEffectId = "Shrine_Test_Curse";
    [SerializeField] private int _testEffectDurationDays = 2;

    private PlayerController _playerController;
    private PlayerNPCInteractionAbility _playerInteraction;
    private UI_ShrineTest _ui;

    public string QuestTitle => _questTitle;
    public string QuestDescription => _questDescription;
    public string CompletionRequestText => _completionRequestText;

    public void RequestInteract(PlayerController player)
    {
        if (player == null) return;

        _playerController = player;
        _playerInteraction = player.GetAbility<PlayerNPCInteractionAbility>();
        _playerController.EnterUIMode();

        OpenShrineUiAsync().Forget();
    }

    private async UniTaskVoid OpenShrineUiAsync()
    {
        if (UIController.Instance == null)
        {
            Debug.LogError($"{nameof(ShrineBuilding)} could not open Shrine UI because {nameof(UIController)} is missing.", this);
            EndInteraction(false);
            return;
        }

        _ui = await UIController.Instance.OpenAsync<UI_ShrineTest>(ui =>
        {
            ui.Configure(this, EndInteractionFromUI);
        });

        if (_ui == null)
        {
            EndInteraction(false);
        }
    }

    public string ApplyTestQuestResult(bool isSuccess)
    {
        if (!IsConstructionComplete) return "제단 건설이 완료되지 않았습니다.";

        if (WorldEffectManager.Instance == null) return "WorldEffectManager를 찾을 수 없어 효과를 적용하지 못했습니다.";

        int durationDays = Mathf.Max(1, _testEffectDurationDays);

        if (isSuccess)
        {
            WorldEffectManager.Instance.AddEffect(_successEffectId, EWorldEffectKind.Buff, durationDays);
            return $"성공: {_successEffectId} 버프가 {durationDays}일 동안 적용되었습니다.";
        }

        WorldEffectManager.Instance.AddEffect(_failureEffectId, EWorldEffectKind.Debuff, durationDays);
        return $"실패: {_failureEffectId} 디버프가 {durationDays}일 동안 적용되었습니다.";
    }

    private void EndInteractionFromUI()
    {
        EndInteraction(false);
    }

    private void EndInteraction(bool closeUi)
    {
        if (closeUi)
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
        EndInteraction(true);
        base.OnDestroy();
    }
}
