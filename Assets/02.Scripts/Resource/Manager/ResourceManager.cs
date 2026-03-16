using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private ILoadSystem _loadSystem;
    private readonly Dictionary<string, Object> _cache = new();

    private void Awake()
    {
        Instance = this;

        _loadSystem = new ResourcesLoadSystem();
    }

    public async UniTask<T> LoadAsync<T>(string key) where T : Object
    {
        if (_cache.TryGetValue(key, out var cached)) return cached as T;

        var asset = await _loadSystem.LoadAsync<T>(key);
        if (asset != null) _cache[key] = asset;

        return asset;
    }

    public T Get<T>(string key) where T : Object
    {
        if (_cache.TryGetValue(key, out var cached)) return cached as T;

        Debug.LogWarning($"[ResourceManager] Asset not loaded: {key}");
        return null;
    }

    public void Release(string key)
    {
        if (_cache.Remove(key)) _loadSystem.Release(key);
    }

    public void ReleaseAll()
    {
        foreach (var key in _cache.Keys)
        {
            _loadSystem.Release(key);
        }
        _cache.Clear();
    }

    public bool IsLoaded(string key)
    {
        return _cache.ContainsKey(key);
    }

    public void SetLoadSystem(ILoadSystem loadSystem)
    {
        _loadSystem = loadSystem;
    }
}
