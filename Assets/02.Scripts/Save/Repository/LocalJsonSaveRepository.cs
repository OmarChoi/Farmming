using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LocalJsonSaveRepository : ISaveRepository
{
    private string GetFilePath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"save_{slot}.json");

    public async UniTask SaveAsync(SaveData data, int slot)
    {
        string json = JsonUtility.ToJson(data, true);
        await File.WriteAllTextAsync(GetFilePath(slot), json);
    }

    public async UniTask<SaveData> LoadAsync(int slot)
    {
        string path = GetFilePath(slot);
        if (!File.Exists(path)) return null;

        string json = await File.ReadAllTextAsync(path);
        return JsonUtility.FromJson<SaveData>(json);
    }

    public UniTask<bool> HasSaveAsync(int slot)
    {
        return UniTask.FromResult(File.Exists(GetFilePath(slot)));
    }

    public UniTask DeleteAsync(int slot)
    {
        string path = GetFilePath(slot);
        if (File.Exists(path))
            File.Delete(path);

        return UniTask.CompletedTask;
    }
}