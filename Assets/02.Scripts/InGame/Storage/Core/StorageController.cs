using System;
using UnityEngine;

/// Storage UI를 직접 제어하지 않고 세션 이벤트만 발행한다.
public class StorageController : MonoBehaviour
{
    public static StorageController Instance { get; private set; }
    public static event Action<StorageController> OnReady;

    private PlayerInventoryAbility _playerInventory;
    private StorageObject _currentStorageObject;

    public bool IsOpen => CurrentSession != null;
    public StorageSession CurrentSession { get; private set; }

    public event Action<StorageSession> OnStorageOpened;
    public event Action OnStorageClosed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        OnReady?.Invoke(this);
    }

    private void OnEnable()
    {
        PlayerInventoryAbility.OnLocalPlayerReady += OnPlayerReady;
        CacheInventoryAbility();
    }

    private void OnDisable()
    {
        PlayerInventoryAbility.OnLocalPlayerReady -= OnPlayerReady;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ClearCurrentSession(notifyUi: true);
            Instance = null;
        }
    }

    private void OnPlayerReady(PlayerInventoryAbility ability)
    {
        if (ability == null) return;
        _playerInventory = ability;
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            CloseStorage();
    }

    public void OpenStorage(StorageObject storageObject)
    {
        if (storageObject == null) return;
        if (!EnsureInventoryAbility()) return;

        StorageTransferService transferService;
        var storage = storageObject.Storage;
        var syncHandler = storageObject.SyncHandler;

        if (syncHandler != null)
        {
            transferService = new NetworkStorageTransferProxy(
                _playerInventory.Domain, storage, syncHandler);
        }
        else
        {
            transferService = new StorageTransferService(_playerInventory.Domain, storage);
        }

        _currentStorageObject = storageObject;
        CurrentSession = new StorageSession(_playerInventory, storageObject, storage, transferService);

        _playerInventory.Open();
        OnStorageOpened?.Invoke(CurrentSession);
    }

    public void CloseStorage()
    {
        if (!IsOpen) return;

        ClearCurrentSession(notifyUi: true);
        _playerInventory?.Close();
    }

    private void ClearCurrentSession(bool notifyUi)
    {
        if (!IsOpen) return;

        if (notifyUi)
            OnStorageClosed?.Invoke();

        _currentStorageObject?.EndInteract();
        _currentStorageObject = null;
        CurrentSession = null;
    }

    private bool EnsureInventoryAbility()
    {
        if (_playerInventory != null) return true;

        CacheInventoryAbility();
        return _playerInventory != null;
    }

    private void CacheInventoryAbility()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in players)
        {
            if (player == null || !player.IsMine) continue;

            PlayerInventoryAbility ability = player.GetAbility<PlayerInventoryAbility>();
            if (ability == null) continue;

            _playerInventory = ability;
            return;
        }
    }
}
