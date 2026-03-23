using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NpcDataContainer", menuName = "Scriptable Objects/Npc/NpcDataContainer")]
public class NpcDataContainer : ScriptableObject
{
    [field: SerializeField] public List<NpcData> NpcData { get; private set; } = new();

    public NpcData GetNpc(string id)
    {
        return NpcData.Find(x => x.NpcId == id);
    }
}
