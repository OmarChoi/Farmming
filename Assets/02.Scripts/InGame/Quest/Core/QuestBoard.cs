using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

// 퀘스트 보드 월드 오브젝트. 상호작용 시 UIController 경유로 UI_QuestBoard를 열고
// UILifecycleActions로 Open/Close 시점의 게임플레이 훅(UI 모드 토글, 상호작용 종료)을 위임한다.
[RequireComponent(typeof(BaseBuilding))]
public class QuestBoard : MonoBehaviour, IWorldInteractable
{
    [Header("퀘스트 보드 컴포넌트")]
    [SerializeField] private QuestBoardDataSO _boardQuest;
    [SerializeField] private QuestMarkService _questMarkService;

    private PlayerController _playerController;
    private PlayerNPCInteractionAbility _playerInteraction;
    private BaseBuilding _building;

    // Open 경로의 예외와 OnClose 콜백이 서로 겹쳐 ExitUIMode/EndInteraction이 중복 호출되는 것을 막는 가드.
    private bool _isInteracting;

    public QuestBoardDataSO BoardQuest => _boardQuest;
    public string AnimationTrigger => string.Empty;

    private void Awake()
    {
        if (DailyQuestManager.Instance != null)
        {
            DailyQuestManager.Instance.SetDailyQuestBoardData(_boardQuest);
        }

        if (_questMarkService == null)
        {
            _questMarkService = FindFirstObjectByType<QuestMarkService>();
        }
        _building = GetComponent<BaseBuilding>();
    }

    public void Interact(PlayerController player)
    {
        if (!_building.IsConstructionComplete) return;
        // 이미 상호작용 중이면 재진입 방지 (UI 열리는 도중 Interact 중복 방지).
        if (_isInteracting) return;

        _playerController = player;
        _playerInteraction = player.GetAbility<PlayerNPCInteractionAbility>();

        _questMarkService?.MarkQuestBoardChecked();

        OpenQuestBoard().Forget();
    }

    private async UniTaskVoid OpenQuestBoard()
    {
        if (DailyQuestManager.Instance == null) return;
        if (UIController.Instance == null) return;

        IReadOnlyList<QuestDataSO> todayQuests = DailyQuestManager.Instance.TodayDailyQuests;
        if (todayQuests == null || todayQuests.Count == 0) return;

        // UI 모드 전환은 OpenAsync 예외와 무관하게 반드시 복구돼야 하므로 try/catch로 감싼다.
        _isInteracting = true;
        _playerController?.EnterUIMode();

        try
        {
            // OnOpen: 데이터 주입 / OnClose: 버튼·Escape 어느 경로든 닫힘 직후 후처리.
            // 두 콜백 모두 1회성이고 UIBase가 다음 Open 사이클 전에 자동 해제한다.
            await UIController.Instance.OpenAsync(new UILifecycleActions<UI_QuestBoard>
            {
                OnOpen = ui => ui.SetQuests(todayQuests),
                OnClose = _ => EndInteract(),
            });
        }
        catch
        {
            // Open 도중 예외가 터지면 OnClose가 발화되지 않을 수 있으므로 여기서 직접 복구.
            EndInteract();
            throw;
        }
    }

    public void EndInteract()
    {
        // OnClose + catch 경로의 중복 호출을 차단.
        if (!_isInteracting) return;
        _isInteracting = false;

        _playerController?.ExitUIMode();
        _playerInteraction?.EndInteraction();
    }
}
