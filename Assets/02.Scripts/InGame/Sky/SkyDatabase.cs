using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkyDatabase", menuName = "Skybox/Database")]
public class SkyDatabase : ScriptableObject
{
    [Serializable]
    private struct Entry
    {
        public ESkyType Type;
        public SkyKeyframeSO Keyframe;
    }

    [SerializeField] 
    private Entry[] _entries;

    [Header("Global Fog")]
    [Range(0f, 1f)] 
    public float FogIntensity;
    [Range(0f, 1f)] 
    public float FogFill;
    public float FogPosition;

    private Dictionary<ESkyType, SkyKeyframeSO> _lookup;

    public SkyKeyframeSO Get(ESkyType type)
    {
        BuildLookup();
        _lookup.TryGetValue(type, out SkyKeyframeSO result);
        return result;
    }

    private void BuildLookup()
    {
        if (_lookup != null) return;

        if (_entries == null)
        {
            _lookup = new Dictionary<ESkyType, SkyKeyframeSO>(); 
            return;
        }

        _lookup = new Dictionary<ESkyType, SkyKeyframeSO>(_entries.Length);
        foreach (Entry entry in _entries)
        {
            _lookup[entry.Type] = entry.Keyframe;
        }
    }

    private void OnEnable() => _lookup = null;
}
