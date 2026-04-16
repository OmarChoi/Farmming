public sealed class StorageSession
{
    public StorageSession(
        PlayerInventoryAbility playerInventory,
        StorageObject storageObject,
        StorageDomain storage,
        StorageTransferService transferService)
    {
        PlayerInventory = playerInventory;
        StorageObject = storageObject;
        Storage = storage;
        TransferService = transferService;
    }

    public PlayerInventoryAbility PlayerInventory { get; }
    public StorageObject StorageObject { get; }
    public StorageDomain Storage { get; }
    public StorageTransferService TransferService { get; }
}
