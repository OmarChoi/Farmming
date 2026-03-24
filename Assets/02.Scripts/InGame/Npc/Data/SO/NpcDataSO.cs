using UnityEngine;

[CreateAssetMenu(fileName = "NpcDataSO", menuName = "Scriptable Objects/Npc/NpcDataSO")]
public class NpcDataSO : ScriptableObject
{
    public string NpcId;
    public string NpcName;

    public GameObject Prefab;

    public NpcDialogueSO[] StartDialogues;
    public NpcDialogueSO[] TalkDialogues;

    public bool UseRandomTimeOffset = true;
    public int MinTimeOffset = 0;
    public int MaxTimeOffset = 10;
}
