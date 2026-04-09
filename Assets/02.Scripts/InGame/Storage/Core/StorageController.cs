using UnityEngine;

/// UI_Inventory + UI_Storage를 조합하고 클릭모드를 전환한다.
/// 네트워크 분기는 NetworkStorageTransferProxy가 내부에서 처리한다.
public class StorageController : MonoBehaviour
{
    [Header("요구 컴포넌트")]
    [SerializeField] private UI_Inventory _uiInventory;
    [SerializeField] private UI_Storage _uiStorage;

    private PlayerInventoryAbility _playerInventory;
    private StorageTransferService _transferService;
    private StorageObject _currentStorageObject;

    private bool _isOpen;

    private void OnEnable()
    {
        PlayerInventoryAbility.OnLocalPlayerReady += OnPlayerReady;
        if (_uiStorage != null)
            _uiStorage.OnCloseRequested += CloseStorage;
    }

    private void OnDisable()
    {
        PlayerInventoryAbility.OnLocalPlayerReady -= OnPlayerReady;
        if (_uiStorage != null)
            _uiStorage.OnCloseRequested -= CloseStorage;
    }

    private void OnPlayerReady(PlayerInventoryAbility ability)
    {
        _playerInventory = ability;
    }

    private void Update()
    {
        if (!_isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            CloseStorage();
    }

    public void OpenStorage(StorageObject storageObject)
    {
        if (_playerInventory == null || storageObject == null) return;

        _currentStorageObject = storageObject;
        var storage = storageObject.Storage;
        var syncHandler = storageObject.SyncHandler;

        if (syncHandler != null)
        {
            _transferService = new NetworkStorageTransferProxy(
                _playerInventory.Domain, storage, syncHandler);
        }
        else
        {
            // SyncHandler 없음 (로컬 전용)
            _transferService = new StorageTransferService(_playerInventory.Domain, storage);
        }

        _uiStorage.Init(storage, _transferService);
        _uiStorage.SetLinkedInventory(_uiInventory);
        _uiInventory.SetLinkedStorage(_uiStorage, _transferService);
        _uiInventory.SetClickMode(EInventoryClickMode.Storage);

        _playerInventory.Open();
        _uiStorage.Open();
        _isOpen = true;
    }

    public void CloseStorage()
    {
        if (!_isOpen) return;

        _uiStorage.Close();
        _uiStorage.SetLinkedInventory(null);
        _uiInventory.SetClickMode(EInventoryClickMode.Normal);
        _uiInventory.SetLinkedStorage(null, null);
        _playerInventory.Close();

        _currentStorageObject?.EndInteract();
        _currentStorageObject = null;
        _transferService = null;
        _isOpen = false;
    }
}