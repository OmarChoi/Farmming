using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MockLoadSystem : ILoadSystem
{
    public UniTask<T> LoadAsync<T>(string key) where T : Object
    {
        return UniTask.FromResult<T>(null);
    }

    public UniTask<IList<T>> LoadAllAsync<T>(string label) where T : Object
    {
        return UniTask.FromResult<IList<T>>(new List<T>());
    }

    public void Release(string key)
    {
    }
}
