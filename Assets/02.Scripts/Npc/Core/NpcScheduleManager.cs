using System.Collections.Generic;
using UnityEngine;

public class NpcScheduleManager : MonoBehaviour
{
    public static NpcScheduleManager Instance { get; private set; }

    private readonly List<NpcController> _npcs = new();

    private void Awake()
    {
        Instance = this;
    }

    public void Register(NpcController npc)
    {
        if (npc == null || _npcs.Contains(npc))
        {
            return;
        }

        _npcs.Add(npc);
    }

    public void Unregister(NpcController npc)
    {
        if (npc == null)
        {
            return;
        }

        _npcs.Remove(npc);
    }
}
