using System.Collections.Generic;
using UnityEngine;

public class NpcLocationManager : MonoBehaviour
{
    public static NpcLocationManager Instance { get; private set; }

    private readonly Dictionary<string, NpcLocationAnchor> _anchorMap = new();

    private void Awake()
    {
        Instance = this;
    }

    private string MakeKey(string npcId, ENpcLocationType type, string locationKey)
    {
        if (string.IsNullOrEmpty(locationKey))
        {
            return MakeDefaultKey(npcId, type);
        }
        return $"{npcId}_{type}_{locationKey}";
    }

    private string MakeDefaultKey(string npcId, ENpcLocationType type)
    {
        return $"{npcId}_{type}";
    }

    public void Register(NpcLocationAnchor anchor)
    {
        if (anchor == null) return;

        string key = MakeKey(anchor.NpcId, anchor.LocationType, anchor.LocationKey);

        _anchorMap[key] = anchor;
    }

    public void Unregister(NpcLocationAnchor anchor)
    {
        if (anchor == null) return;

        string key = MakeKey(anchor.NpcId, anchor.LocationType, anchor.LocationKey);

        if (_anchorMap.TryGetValue(key, out var current))
        {
            if (current == anchor)
            {
                _anchorMap.Remove(key);
            }
        }
    }

    public bool TryGetLocation(string npcId, ENpcLocationType type, string locationKey, out Vector3 position)
    {
        // 상세 위치를 먼저 찾습니다.
        if (!string.IsNullOrEmpty(locationKey))
        {
            string key = MakeKey(npcId, type, locationKey);

            if (_anchorMap.TryGetValue(key, out var anchor) && anchor != null)
            {
                position = anchor.Position;
                return true;
            }
        }

        // 기본 위치로 폴백을 시도합니다.
        string defaultKey = MakeDefaultKey(npcId, type);

        if (_anchorMap.TryGetValue(defaultKey, out var defaultAnchor) && defaultAnchor != null)
        {
            position = defaultAnchor.Position;
            return true;
        }

        position = Vector3.zero;
        return false;
    }
}
