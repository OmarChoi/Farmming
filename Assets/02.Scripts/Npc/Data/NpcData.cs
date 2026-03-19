using System;

[Serializable]
public class NpcData
{
    // Npc id와 이름 관련 정보입니다.
    public string NpcId;
    public string NpcName;

    // Npc 대화 관련 정보입니다.
    public NpcDialogueSO[] StartDialogues; // 처음에 인사할 때 나오는 대화 문구입니다.
    public NpcDialogueSO[] TalkDialogues;  // 대화 선택 시 나오는 대화 문구입니다.

    // Npc 일정(스케줄) 시간 오프셋 관련 정보입니다.
    public bool UseRandomTimeOffset = true;
    public int MinTimeOffset = 0;
    public int MaxTimeOffset = 10;
}
