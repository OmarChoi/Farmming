using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NpcDataContainerSO", menuName = "Scriptable Objects/Npc/NpcDataContainer")]
public class NpcDataContainerSO : ScriptableObject
{
    [Header("Npc 리스트")]
    [SerializeField] private List<NpcDataSO> _npcList;

    private Dictionary<string, NpcDataSO> _map;

    public NpcDataSO GetNpc(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        EnsureMap();
        _map.TryGetValue(id, out var result);
        return result;
    }

    public string GetNpcName(string id)
    {
        NpcDataSO npc = GetNpc(id);
        return npc != null ? npc.NpcName : id;
    }

    private void EnsureMap()
    {
        if (_map != null) return;

        _map = new Dictionary<string, NpcDataSO>();

        foreach (var npc in _npcList)
        {
            if (npc == null || string.IsNullOrEmpty(npc.NpcId)) continue;
            _map[npc.NpcId] = npc;
        }
    }

    private void OnEnable()
    {
        _map = null;
    }
}
