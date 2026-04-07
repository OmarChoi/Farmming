using System.Collections.Generic;
using UnityEngine;

public class NpcLocationManager : MonoBehaviour
{
    public static NpcLocationManager Instance { get; private set; }

    private readonly Dictionary<string, NpcLocationAnchor> _anchorMap = new();
    private readonly Dictionary<string, List<NpcLocationAnchor>> _anchorsByNpcId = new();

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

        if (string.IsNullOrEmpty(anchor.NpcId)) return;

        if (!_anchorsByNpcId.TryGetValue(anchor.NpcId, out List<NpcLocationAnchor> list))
        {
            list = new List<NpcLocationAnchor>();
            _anchorsByNpcId.Add(anchor.NpcId, list);
        }

        if (!list.Contains(anchor))
        {
            list.Add(anchor);
        }
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

        if (string.IsNullOrEmpty(anchor.NpcId)) return;

        if (_anchorsByNpcId.TryGetValue(anchor.NpcId, out List<NpcLocationAnchor> list))
        {
            list.Remove(anchor);

            if (list.Count == 0)
            {
                _anchorsByNpcId.Remove(anchor.NpcId);
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

    public bool TryGetAnchor(string npcId, ENpcLocationType type, string locationKey, out NpcLocationAnchor anchor)
    {
        if (!string.IsNullOrEmpty(locationKey))
        {
            string key = MakeKey(npcId, type, locationKey);

            if (_anchorMap.TryGetValue(key, out anchor) && anchor != null) return true;
        }

        string defaultKey = MakeDefaultKey(npcId, type);

        if (_anchorMap.TryGetValue(defaultKey, out anchor) && anchor != null) return true;

        anchor = null;
        return false;
    }

    public bool TryGetFirstAnchor(string npcId, out NpcLocationAnchor anchor)
    {
        if (!string.IsNullOrEmpty(npcId) && _anchorsByNpcId.TryGetValue(npcId, out List<NpcLocationAnchor> list))
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;

                anchor = list[i];
                return true;
            }
        }

        anchor = null;
        return false;
    }
}
