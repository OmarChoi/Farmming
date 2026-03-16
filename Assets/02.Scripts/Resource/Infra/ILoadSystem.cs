using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ILoadSystem
{
    UniTask<T> LoadAsync<T>(string key) where T : Object;
    void Release(string key);
}
