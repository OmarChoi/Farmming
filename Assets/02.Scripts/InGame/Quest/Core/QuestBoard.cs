using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using static AssetKey;

public class QuestBoard : MonoBehaviour, IInteraction
{
    [Header("퀘스트 보드 컴포넌트")]
    [SerializeField] private QuestBoardDataSO _boardQuest;
    [SerializeField] private UI_QuestBoard _uiQuestBoard;

    private PlayerController _playerController;
    private PlayerNPCInteractionAbility _playerInteraction;

    public QuestBoardDataSO BoardQuest => _boardQuest;

    private void Start()
    {
        if (DailyQuestManager.Instance != null)
        {
            DailyQuestManager.Instance.SetDailyQuestBoardData(_boardQuest);
        }
    }

    private void OnEnable()
    {
        if (_uiQuestBoard != null)
        {
            _uiQuestBoard.OnCloseRequested += CloseQuestBoard;
        }
    }

    private void OnDisable()
    {
        if (_uiQuestBoard != null)
        {
            _uiQuestBoard.OnCloseRequested -= CloseQuestBoard;
        }
    }

    public void RequestInteract(PlayerController player)
    {
        _playerController = player;
        _playerInteraction = player.GetAbility<PlayerNPCInteractionAbility>();

        OpenQuestBoard().Forget();
    }

    public async UniTaskVoid OpenQuestBoard()
    {
        if (DailyQuestManager.Instance == null) return;

        var todayQuests = DailyQuestManager.Instance.TodayDailyQuests;

        if (todayQuests == null || todayQuests.Count == 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning("오늘 표시할 일일 퀘스트가 없습니다.");
#endif
            return;
        }

        await _uiQuestBoard.OpenAsync(new List<QuestDataSO>(todayQuests));
        if (_playerController != null)
        {
            _playerController?.SetCursorLock(false);
        }
    }

    public async void CloseQuestBoard()
    {
        await _uiQuestBoard.CloseAsync();
        if (_playerController != null)
        {
            _playerController?.SetCursorLock(true);
        }
        _playerInteraction?.EndInteraction();
    }
}
