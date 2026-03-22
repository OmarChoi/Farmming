using UnityEngine;

[CreateAssetMenu(fileName = "QuestDataSO", menuName = "Scriptable Objects/Quest/QuestDataSO")]
public class QuestDataSO : ScriptableObject
{
    // 퀘스트 ID와 기본 정보입니다.
    public string QuestId;
    public string QuestName;
    public string Description;

    // 퀘스트의 목적 종류(자원 캐기, 아이템 전달 등)와 관련된 정보입니다.
    public EQuestObjectiveType ObjectiveType;
    public string TargetId;         // 퀘스트 목표 판정 대상(자원 캐기의 Tree, Rock 등)의 Id입니다.
    public int RequiredAmount = 1;

    private void OnValidate()
    {
        // 실수로 퀘스트 요구치가 0 이하로 지정될 경우, 자동으로 1로 바꿔주어 오류를 방어합니다.
        if (RequiredAmount < 1)
        {
            RequiredAmount = 1;
        }
    }

    // 퀘스트 보상 관련 정보입니다.
    public QuestRewardData Reward;

    // 퀘스트 게시판에 뜰 수 있는 퀘스트인지 판단해줍니다.
    public bool CanAppearOnBoard = true;
}
