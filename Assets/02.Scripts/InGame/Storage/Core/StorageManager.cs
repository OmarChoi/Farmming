using System.Collections.Generic;
using UnityEngine;

public sealed class StorageManager
{
    private static readonly StorageManager _instance = new();

    public static StorageManager Instance => _instance;

    private readonly StorageRegistry _registry = new();
    private readonly Dictionary<string, StorageSaveData> _pending = new();

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance._registry.Clear();
        _instance._pending.Clear();
    }
#endif

    public static string CreateKey(string buildingId, Vector3Int anchor)
        => $"{buildingId}:{anchor.x}:{anchor.y}:{anchor.z}";

    public void RegisterBuildingStorage(BuildingSaveData buildingSaveData, StorageObject storageObject)
    {
        if (buildingSaveData == null || storageObject == null) return;

        var anchor = new Vector3Int(buildingSaveData.AnchorX, buildingSaveData.AnchorY, buildingSaveData.AnchorZ);
        string key = CreateKey(buildingSaveData.BuildingId, anchor);

        storageObject.BindToBuilding(buildingSaveData.BuildingId, anchor);
        _registry.Register(key, storageObject);

        if (_pending.TryGetValue(key, out StorageSaveData saveData))
        {
            storageObject.ImportSaveData(saveData);
            _pending.Remove(key);
        }
    }

    public void Unregister(StorageObject storageObject)
    {
        if (storageObject == null || !storageObject.TryGetStorageKey(out string key)) return;
        if (_registry.TryGet(key, out StorageObject registered) && registered == storageObject)
            _registry.Remove(key);
    }

    public List<StorageSaveData> ExportStorages()
    {
        var list = new List<StorageSaveData>();

        foreach (KeyValuePair<string, StorageObject> kvp in _registry.AllStorages)
        {
            StorageObject storageObject = kvp.Value;
            if (storageObject == null) continue;

            StorageSaveData saveData = storageObject.ExportSaveData();
            if (saveData != null)
                list.Add(saveData);
        }

        return list;
    }

    public void ImportStorages(List<StorageSaveData> storages)
    {
        _pending.Clear();

        if (storages == null) return;

        foreach (StorageSaveData saveData in storages)
        {
            if (saveData == null) continue;

            string key = CreateKey(
                saveData.BuildingId,
                new Vector3Int(saveData.AnchorX, saveData.AnchorY, saveData.AnchorZ));

            if (_registry.TryGet(key, out StorageObject storageObject) && storageObject != null)
            {
                storageObject.ImportSaveData(saveData);
            }
            else
            {
                _pending[key] = saveData;
            }
        }
    }

    public void ClearPending()
        => _pending.Clear();

    public void ClearAll()
    {
        _registry.Clear();
        _pending.Clear();
    }
}