using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class QuestBoard : MonoBehaviour
{
    [Header("퀘스트 보드 컴포넌트")]
    [SerializeField] private QuestBoardDataSO _boardQuest;
    [SerializeField] private UI_QuestBoard _uiQuestBoard;

    [Header("플레이어 컨트롤러")]
    [SerializeField] private PlayerController _playerController;

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

    public void Interact()
    {
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
        _playerController?.SetCursorLock(false);
    }

    public async void CloseQuestBoard()
    {
        await _uiQuestBoard.CloseAsync();
        _playerController?.SetCursorLock(true);
    }
}
