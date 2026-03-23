using UnityEngine;

public class QuestJournalController : MonoBehaviour
{
    [Header("퀘스트 창 UI")]
    [SerializeField] private UI_QuestJournal _uiQuestJournal;

    [Header("플레이어 컨트롤러")]
    [SerializeField] private PlayerController _playerController;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleQuestJournal();
        }
    }

    public void ToggleQuestJournal()
    {
        if (_uiQuestJournal == null) return;

        if (_uiQuestJournal.IsOpen())
        {
            CloseQuestJournal();
        }
        else
        {
            OpenQuestJournal();
        }
    }

    public void OpenQuestJournal()
    {
        if (_uiQuestJournal == null) return;

        _uiQuestJournal.Open();
        _playerController?.SetCursorLock(false);
    }

    public void CloseQuestJournal()
    {
        if (_uiQuestJournal == null) return;

        _uiQuestJournal.Close();
        _playerController?.SetCursorLock(true);
    }
}
