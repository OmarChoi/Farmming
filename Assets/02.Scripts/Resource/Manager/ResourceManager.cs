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
        _loadSystem = new AddressablesLoadSystem();
    }

    public async UniTask<T> LoadAsync<T>(string key) where T : Object
    {
        if (_cache.TryGetValue(key, out Object cached)) return cached as T;

        T asset = await _loadSystem.LoadAsync<T>(key);
        if (asset != null) _cache[key] = asset;

        return asset;
    }

    public async UniTask<IList<T>> LoadAllAsync<T>(string label) where T : Object
    {
        return await _loadSystem.LoadAllAsync<T>(label);
    }

    public void Release(string key)
    {
        if (_cache.Remove(key)) _loadSystem.Release(key);
    }

    public void ReleaseAll()
    {
        foreach (string key in _cache.Keys) _loadSystem.Release(key);
        _cache.Clear();
    }
}