using System;

[Serializable]
public class NpcData
{
    // Npc id와 이름 관련 정보입니다.
    public string NpcId;
    public string NpcName;

    // Npc 대화 관련 정보입니다.
    public string[] StartDialogues;
    public string[] TalkDialogues;

    // Npc 일정(스케줄) 시간 오프셋 관련 정보입니다.
    public bool UseRandomTimeOffset = true;
    public int MinTimeOffset = 0;
    public int MaxTimeOffset = 10;
}
