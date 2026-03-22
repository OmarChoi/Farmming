using UnityEngine;

[CreateAssetMenu(fileName = "NpcDialogueSO", menuName = "Scriptable Objects/Npc/NpcDialogueSO")]
public class NpcDialogueSO : ScriptableObject
{
    public string Id;  // 대화 구별용 Id입니다.
    public NpcDialogueLine[] Lines;
}

[System.Serializable]
public class NpcDialogueLine
{
    public string Text;
    public NpcDialogueSO NextDialogue;  // 분기 대화가 필요할 때 사용할 수 있습니다.
}