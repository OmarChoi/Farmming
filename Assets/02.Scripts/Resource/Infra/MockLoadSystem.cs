using Cysharp.Threading.Tasks;
using UnityEngine;

public class MockLoadSystem : ILoadSystem
{
    public UniTask<T> LoadAsync<T>(string key) where T : Object
    {
        return UniTask.FromResult<T>(null);
    }

    public void Release(string key)
    {
    }
}
