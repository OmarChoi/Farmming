using UnityEngine;

public class QuestBoard : MonoBehaviour
{
    [SerializeField] private QuestDataSO _boardQuest;

    public QuestDataSO BoardQuest => _boardQuest;

    public void Interact()
    {
        var questManager = QuestManager.Instance;
        if (questManager == null) return;

        if (!questManager.HasActiveQuest())
        {
            // todo. 여기서 UI 열기
            return;
        }

        if (questManager.CurrentQuest.QuestData == _boardQuest && questManager.CanCompleteQuest())
        {
            bool success = questManager.CompleteQuest();
            if (success)
            {
#if UNITY_EDITOR
                Debug.Log("퀘스트 완료!");
#endif
            }
            return;
        }
#if UNITY_EDITOR
        Debug.Log("이미 진행 중인 퀘스트가 있습니다.");
#endif
    }

    public bool AcceptBoardQuest()
    {
        return QuestManager.Instance.AcceptQuest(_boardQuest);
    }
}
