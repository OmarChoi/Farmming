using UnityEngine;
using System.Collections.Generic;

public class NpcRegistry : MonoBehaviour
{
    public static NpcRegistry Instance { get; private set; }

    private readonly Dictionary<string, NpcController> _npcMap = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool TryGet(string npcId, out NpcController controller)
    {
        if (string.IsNullOrEmpty(npcId))
        {
            controller = null;
            return false;
        }

        if (_npcMap.TryGetValue(npcId, out controller))
        {
            if (controller != null) return true;
            _npcMap.Remove(npcId);
        }

        controller = null;
        return false;
    }

    public void Register(string npcId, NpcController controller)
    {
        if (string.IsNullOrEmpty(npcId) || controller == null) return;
        _npcMap[npcId] = controller;
    }

    public void Unregister(string npcId, NpcController controller)
    {
        if (string.IsNullOrEmpty(npcId) || controller == null) return;

        if (_npcMap.TryGetValue(npcId, out var current) && current == controller)
        {
            _npcMap.Remove(npcId);
        }
    }
}
