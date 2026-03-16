using System.IO;
using UnityEngine;

public class LocalJsonSaveRepository : ISaveRepository
{
    private string GetFilePath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"save_{slot}.json");

    public void Save(SaveData data, int slot)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetFilePath(slot), json);
    }

    public SaveData Load(int slot)
    {
        string path = GetFilePath(slot);
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SaveData>(json);
    }

    public bool HasSave(int slot) => File.Exists(GetFilePath(slot));

    public void Delete(int slot)
    {
        string path = GetFilePath(slot);
        if (File.Exists(path))
            File.Delete(path);
    }
}