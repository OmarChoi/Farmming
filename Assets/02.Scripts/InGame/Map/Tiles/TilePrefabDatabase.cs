using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/Tile Prefab Database")]
public class TilePrefabDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ETileType TileType;
        public GameObject CellPrefab;
    }

    [SerializeField] private Entry[] _entries;

    private Dictionary<ETileType, GameObject> _lookup;

    public GameObject GetPrefab(ETileType type)
    {
        _lookup ??= BuildLookup();
        return _lookup.TryGetValue(type, out var prefab) ? prefab : null;
    }

    private Dictionary<ETileType, GameObject> BuildLookup()
    {
        var dict = new Dictionary<ETileType, GameObject>();
        foreach (var e in _entries)
            dict[e.TileType] = e.CellPrefab;
        return dict;
    }
}