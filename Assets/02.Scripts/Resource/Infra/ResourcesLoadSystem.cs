using Cysharp.Threading.Tasks;
using UnityEngine;

public class ResourcesLoadSystem : ILoadSystem
{
    public UniTask<T> LoadAsync<T>(string key) where T : Object
    {
        var asset = Resources.Load<T>(key);
        return UniTask.FromResult(asset);
    }

    public void Release(string key)
    {
    }
}
