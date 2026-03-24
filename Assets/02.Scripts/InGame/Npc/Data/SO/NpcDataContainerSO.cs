using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NpcDataContainerSO", menuName = "Scriptable Objects/Npc/NpcDataContainer")]
public class NpcDataContainerSO : ScriptableObject
{
    [SerializeField] private List<NpcDataSO> _npcList;

    private Dictionary<string, NpcDataSO> _map;

    public NpcDataSO GetNpc(string id)
    {
        if (_map == null)
        {
            _map = new();
            foreach (var npc in _npcList)
                _map[npc.NpcId] = npc;
        }

        return _map.TryGetValue(id, out var result) ? result : null;
    }
}
