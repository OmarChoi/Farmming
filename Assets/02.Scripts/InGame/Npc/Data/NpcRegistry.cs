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

    public bool TryGet(string runtimeNpcKey, out NpcController controller)
    {
        if (string.IsNullOrEmpty(runtimeNpcKey))
        {
            controller = null;
            return false;
        }

        if (_npcMap.TryGetValue(runtimeNpcKey, out controller))
        {
            if (controller != null) return true;
            _npcMap.Remove(runtimeNpcKey);
        }

        controller = null;
        return false;
    }

    public void Register(string runtimeNpcKey, NpcController controller)
    {
        if (string.IsNullOrEmpty(runtimeNpcKey) || controller == null) return;
        _npcMap[runtimeNpcKey] = controller;
    }

    public void Unregister(string runtimeNpcKey, NpcController controller)
    {
        if (string.IsNullOrEmpty(runtimeNpcKey) || controller == null) return;

        if (_npcMap.TryGetValue(runtimeNpcKey, out var current) && current == controller)
        {
            _npcMap.Remove(runtimeNpcKey);
        }
    }
}
