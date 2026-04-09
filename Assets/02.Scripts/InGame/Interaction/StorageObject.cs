using UnityEngine;

/// 월드에 배치되는 창고 오브젝트. IWorldInteractable 구현.
/// PUN2 동기화는 StorageSyncHandler에 위임한다.
public class StorageObject : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private int _slotCount = 24;
    [SerializeField] private string _animationTrigger = "StorageOpen";

    private StorageDomain _storage;
    private StorageSyncHandler _syncHandler;
    private StorageController _controller;

    public string AnimationTrigger => _animationTrigger;
    public StorageDomain Storage => _storage;
    public StorageSyncHandler SyncHandler => _syncHandler;

    private void Awake()
    {
        _storage = new StorageDomain(_slotCount);
        _syncHandler = GetComponent<StorageSyncHandler>();
        _syncHandler?.Init(_storage, _slotCount);
        _controller = FindFirstObjectByType<StorageController>();
    }

    public void Interact(PlayerController player)
    {
        _syncHandler?.RequestFullSync();
        _controller?.OpenStorage(this);
    }

    public void EndInteract() { }
}