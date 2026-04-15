using UnityEngine;

[CreateAssetMenu(fileName = "NpcDataSO", menuName = "Scriptable Objects/Npc/NpcDataSO")]
public class NpcDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string NpcId;
    public string NpcName;

    [Header("AI 대화 가능 여부")]
    public bool CanUseAiDialogue = true;

    [Header("AI 대화 성격 정보")]
    public string NpcMbti;
    public string NpcPersonality;
    public string Age;
    public string Job;
    public string WayOfTone;

    [Header("프리팹")]
    public GameObject Prefab;

    [Header("이동 속도")]
    public float WalkSpeed;
    public float RunSpeed;
    public float JumpDuration;
    public float JumpHeight;
    
    [Header("로컬 대화")]
    public NpcDialogueSO[] GreetDialogues;
    public NpcDialogueSO[] TalkDialogues;
    public NpcDialogueSO CuringAskDialogue;
    public NpcDialogueSO CuringAcceptDialogue;
    public NpcDialogueSO CuringDeclineDialogue;
    public NpcDialogueSO CuringAlreadyDoneDialogue;

    [Header("스케줄 시간 오프셋")]
    public bool UseRandomTimeOffset = true;
    public int MinTimeOffset = 0;
    public int MaxTimeOffset = 10;

    [Header("자동 퀘스트 상호작용 여부")]
    public bool AutoStartQuestOnInteract = false;
}
