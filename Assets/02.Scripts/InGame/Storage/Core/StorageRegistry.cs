using System.Collections.Generic;

public sealed class StorageRegistry
{
    private readonly Dictionary<string, StorageObject> _storages = new();

    public IEnumerable<KeyValuePair<string, StorageObject>> AllStorages => _storages;

    public void Register(string key, StorageObject storageObject)
    {
        if (string.IsNullOrEmpty(key) || storageObject == null) return;
        _storages[key] = storageObject;
    }

    public bool TryGet(string key, out StorageObject storageObject)
    {
        if (_storages.TryGetValue(key, out storageObject))
        {
            // 씬 전환 등으로 Destroy된 참조 정리
            if (storageObject == null)
            {
                _storages.Remove(key);
                return false;
            }
            return true;
        }
        return false;
    }

    public bool Remove(string key)
        => !string.IsNullOrEmpty(key) && _storages.Remove(key);

    public void Clear()
        => _storages.Clear();
}