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
        OpenQuestBoard(_boardQuest);
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
