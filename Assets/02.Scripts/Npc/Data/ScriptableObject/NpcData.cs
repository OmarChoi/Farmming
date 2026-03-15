using UnityEngine;

[CreateAssetMenu(fileName = "NpcData", menuName = "Scriptable Objects/Npc/NpcData")]
public class NpcData : ScriptableObject
{
    [Header("Npc 데이터 관련")]
    public string NpcId;
    public string NpcName;

    [Header("Npc 시간 오프셋 관련")]
    public bool UseRandomTimeOffset = true;
    public int MinTimeOffset = 0;
    public int MaxTimeOffset = 10;
}
