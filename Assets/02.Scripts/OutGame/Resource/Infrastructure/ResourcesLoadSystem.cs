using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ResourcesLoadSystem : ILoadSystem
{
    public UniTask<T> LoadAsync<T>(string key) where T : Object
    {
        T asset = Resources.Load<T>(key);
        return UniTask.FromResult(asset);
    }

    public UniTask<IList<T>> LoadAllAsync<T>(string label) where T : Object
    {
        throw new System.NotSupportedException("[ResourcesLoadSystem] Label-based loading is not supported.");
    }

    public void Release(string key)
    {
    }
}
