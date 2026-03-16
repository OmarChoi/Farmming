using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesLoadSystem : ILoadSystem
{
    private readonly Dictionary<string, AsyncOperationHandle> _handles = new();

    public async UniTask<T> LoadAsync<T>(string key) where T : Object
    {
        var handle = Addressables.LoadAssetAsync<T>(key);
        var asset = await handle.ToUniTask();
        _handles[key] = handle;
        return asset;
    }

    public void Release(string key)
    {
        if (_handles.TryGetValue(key, out var handle))
        {
            Addressables.Release(handle);
            _handles.Remove(key);
        }
    }
}
