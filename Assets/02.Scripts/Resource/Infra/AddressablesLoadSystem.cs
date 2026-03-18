using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


public class AddressablesLoadSystem : ILoadSystem
{
    private readonly Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();

    public async UniTask<T> LoadAsync<T>(string key) where T : Object
    {
        try
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
            T asset = await handle.ToUniTask();
            _handles[key] = handle;
            return asset;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AddressablesLoadSystem] Failed to load: {key}\n{e}");
            return null;
        }
    }

    public async UniTask<IList<T>> LoadAllAsync<T>(string label) where T : Object
    {
        try
        {
            AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(label, null);
            IList<T> assets = await handle.ToUniTask();
            _handles[label] = handle;
            return assets;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AddressablesLoadSystem] Failed to load label: {label}\n{e}");
            return null;
        }
    }

    public void Release(string key)
    {
        if (_handles.TryGetValue(key, out AsyncOperationHandle handle))
        {
            Addressables.Release(handle);
            _handles.Remove(key);
        }
    }
}
