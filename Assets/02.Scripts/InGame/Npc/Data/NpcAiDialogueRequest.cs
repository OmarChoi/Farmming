using UnityEngine;

public class NpcAiDialogueRequest
{
    public string NpcId;
    public string NpcName;
    public string Mbti;
    public string Personality;
    public string Job;
    public string PlayerInput;
    public string PlayerId;
    // public int Friendship;
    // public string FriendshipStep;
    public bool IsGreeting;  // 첫 대화인지, 아니면 대화 진행 도중인지 확인하는 용도입니다.
}
