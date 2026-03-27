using UnityEngine;
using System.Collections.Generic;

public class NpcAiDialogueRequest
{
    public string NpcId;
    public string NpcName;
    public string Mbti;
    public string Personality;
    public string Age;
    public string Job;
    public string WayOfTone;
    public string Context;
    public string PlayerInput;
    public string PlayerId;
    public int Friendship;
    public string FriendshipStep;
    public bool IsGreeting;  // 첫 대화인지, 아니면 대화 진행 도중인지 확인하는 용도입니다.

    [Header("기억")]
    public string RollingSummary;
    public List<string> RelevantMemories = new();
}
