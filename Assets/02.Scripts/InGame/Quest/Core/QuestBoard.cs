using UnityEngine;

public class QuestBoard : MonoBehaviour
{
    [Header("퀘스트 보드 컴포넌트")]
    [SerializeField] private QuestBoardDataSO _boardQuest;
    [SerializeField] private UI_QuestBoard _uiQuestBoard;

    [Header("플레이어 컨트롤러")]
    [SerializeField] private PlayerController _playerController;

    public QuestBoardDataSO BoardQuest => _boardQuest;

    private void OnEnable()
    {
        _uiQuestBoard.OnCloseRequested += CloseQuestBoard;
    }

    private void OnDisable()
    {
        _uiQuestBoard.OnCloseRequested -= CloseQuestBoard;
    }

    public void Interact()
    {
        var questManager = QuestManager.Instance;
        if (questManager == null) return;

        if (!questManager.HasActiveQuest())
        {
            OpenQuestBoard(_boardQuest);
            return;
        }

        if (questManager.CanCompleteQuest())
        {
            bool success = questManager.CompleteQuest();
            if (success)
            {
#if UNITY_EDITOR
                Debug.Log("퀘스트 완료!");
#endif
            }
            questManager.ClearCompletedQuest();
            OpenQuestBoard(_boardQuest);
            return;
        }
#if UNITY_EDITOR
        Debug.Log("이미 진행 중인 퀘스트가 있습니다.");
#endif
    }

    public void OpenQuestBoard(QuestBoardDataSO quests)
    {
        if (quests == null || quests.AllQuests == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("열 수 있는 퀘스트 데이터가 없습니다.");
#endif
            return;
        }
        _uiQuestBoard.Open(quests);
        _playerController?.SetCursorLock(false);
    }

    public void CloseQuestBoard()
    {
        _uiQuestBoard.Close();
        _playerController?.SetCursorLock(true);
    }
}
