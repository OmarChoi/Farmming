using System.Collections.Generic;
using UnityEngine;

public class NpcLocationManager : MonoBehaviour
{
    public static NpcLocationManager Instance { get; private set; }

    private readonly Dictionary<string, NpcLocationAnchor> _anchorMap = new();
    private readonly Dictionary<string, List<NpcLocationAnchor>> _anchorsByRuntimeKey = new();

    private void Awake()
    {
        Instance = this;
    }

    private string MakeKey(string runtimeNpcKey, ENpcLocationType type, string locationKey)
    {
        if (string.IsNullOrEmpty(locationKey))
        {
            return MakeDefaultKey(runtimeNpcKey, type);
        }
        return $"{runtimeNpcKey}_{type}_{locationKey}";
    }

    private string MakeDefaultKey(string runtimeNpcKey, ENpcLocationType type)
    {
        return $"{runtimeNpcKey}_{type}";
    }

    public void Register(NpcLocationAnchor anchor)
    {
        if (anchor == null) return;

        string runtimeKey = anchor.RuntimeNpcKey;
        string key = MakeKey(runtimeKey, anchor.LocationType, anchor.LocationKey);
        _anchorMap[key] = anchor;
        Debug.Log($"[NpcLocationManager.Register] runtimeKey={runtimeKey}, type={anchor.LocationType}, locationKey={anchor.LocationKey}, finalKey={key}");
        if (string.IsNullOrEmpty(runtimeKey)) return;

        if (!_anchorsByRuntimeKey.TryGetValue(runtimeKey, out List<NpcLocationAnchor> list))
        {
            list = new List<NpcLocationAnchor>();
            _anchorsByRuntimeKey.Add(runtimeKey, list);
        }

        if (!list.Contains(anchor))
        {
            list.Add(anchor);
        }
    }

    public void Unregister(NpcLocationAnchor anchor)
    {
        if (anchor == null)
        {
            return;
        }

        string runtimeKey = anchor.RuntimeNpcKey;
        string key = MakeKey(runtimeKey, anchor.LocationType, anchor.LocationKey);

        if (_anchorMap.TryGetValue(key, out var current))
        {
            if (current == anchor)
            {
                _anchorMap.Remove(key);
            }
        }

        if (string.IsNullOrEmpty(runtimeKey)) return;

        if (_anchorsByRuntimeKey.TryGetValue(runtimeKey, out List<NpcLocationAnchor> list))
        {
            list.Remove(anchor);

            if (list.Count == 0)
            {
                _anchorsByRuntimeKey.Remove(anchor.RuntimeNpcKey);
            }
        }
    }

    public bool TryGetLocation(string runtimeNpcKey, ENpcLocationType type, string locationKey, out Vector3 position)
    {
        // 상세 위치를 먼저 찾습니다.
        if (!string.IsNullOrEmpty(locationKey))
        {
            string key = MakeKey(runtimeNpcKey, type, locationKey);

            if (_anchorMap.TryGetValue(key, out var anchor) && anchor != null)
            {
                position = anchor.Point.position;
                return true;
            }
        }

        // 기본 위치로 폴백을 시도합니다.
        string defaultKey = MakeDefaultKey(runtimeNpcKey, type);

        if (_anchorMap.TryGetValue(defaultKey, out var defaultAnchor) && defaultAnchor != null)
        {
            position = defaultAnchor.Point.position;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    public bool TryGetAnchor(string runtimeNpcKey, ENpcLocationType type, string locationKey, out NpcLocationAnchor anchor)
    {
        if (!string.IsNullOrEmpty(locationKey))
        {
            string key = MakeKey(runtimeNpcKey, type, locationKey);
            if (_anchorMap.TryGetValue(key, out anchor) && anchor != null) return true;
        }

        string defaultKey = MakeDefaultKey(runtimeNpcKey, type);
        if (_anchorMap.TryGetValue(defaultKey, out anchor) && anchor != null) return true;

        anchor = null;
        return false;
    }

    public bool TryGetFirstAnchor(string runtimeNpcKey, out NpcLocationAnchor anchor)
    {
        if (!string.IsNullOrEmpty(runtimeNpcKey) &&
            _anchorsByRuntimeKey.TryGetValue(runtimeNpcKey, out List<NpcLocationAnchor> list))
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
