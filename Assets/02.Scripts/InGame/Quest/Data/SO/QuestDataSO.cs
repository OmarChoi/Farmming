using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestDataSO", menuName = "Scriptable Objects/Quest/QuestDataSO")]
public class QuestDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string QuestId;
    public string QuestName;
    public string Description;

    [Header("퀘스트 종류")]
    public EQuestCategory QuestCategory;
    public bool IsTutorial = false;

    [Header("수락 / 완료 NPC")]
    public string StartNpcId;       // 이 퀘스트를 주는 NPC입니다.
    public string CompleteNpcId;    // 이 퀘스트를 완료 처리하는 NPC입니다.
    public bool IsForcedAccept = false;  // true면 플레이어가 퀘스트를 강제로 수락하도록 합니다.

    [Header("목표")]
    public EQuestObjectiveType ObjectiveType;
    public string TargetId;   // 목표 대상 Id입니다.
    public List<QuestItemRequirementEntry> ItemRequirements;  // CollectItem와 DeliverItem 단일 아이템용 Id입니다.
    public string TargetNpcId;      // TalkToNpc와 DeliverItem용 Id입니다.
    public int RequiredAmount = 1;  // 단일 목표용 요구 수량입니다.

    [Header("선행 조건")]
    public List<QuestDataSO> PrerequisiteQuests;
    public int RequiredFriendship;

    [Header("보상")]
    public QuestRewardData Reward;

    [Header("NPC 대사")]
    public NpcDialogueSO AcceptDialogue;
    public NpcDialogueSO AcceptResultDialogue;
    public NpcDialogueSO DeclineDialogue;
    public NpcDialogueSO InProgressDialogue;
    public NpcDialogueSO CompleteDialogue;
    public NpcDialogueSO NoQuestDialogue;

    [Header("Info Page")]
    public InfoPageSetSO AcceptInfoPageSet;
    public InfoPageSetSO CompleteInfoPageSet;

    // 실수로 퀘스트 요구치가 0 이하로 지정될 경우, 자동으로 1로 바꿔주어 오류를 방어합니다.
    private void OnValidate()
    {
        if (RequiredAmount < 1)
        {
            RequiredAmount = 1;
        }
        if (ItemRequirements != null)
        {
            for (int i = 0; i < ItemRequirements.Count; i++)
            {
                OnValidateItemRequirements(i);
            }
        }
    }

    private void OnValidateItemRequirements(int count)
    {
        if (ItemRequirements[count].Amount < 1)
        {
            ItemRequirements[count] = ItemRequirements[count].WithAmount(1);
        }
    }

    public bool HasMultipleItemRequirements => ItemRequirements != null && ItemRequirements.Count > 0;

    public bool UsesItemRequirementList()
    {
        return ObjectiveType == EQuestObjectiveType.CollectItem ||
               ObjectiveType == EQuestObjectiveType.DeliverItem
               ? HasMultipleItemRequirements
               : false;
    }
}
