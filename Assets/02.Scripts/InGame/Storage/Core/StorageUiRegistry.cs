using System;
using UnityEngine;

public static class StorageUiRegistry
{
    public static UI_Storage CurrentStorage { get; private set; }
    public static UI_Inventory CurrentInventory { get; private set; }

    public static event Action<UI_Storage> OnStorageRegistered;
    public static event Action OnStorageUnregistered;
    public static event Action<UI_Inventory> OnInventoryRegistered;
    public static event Action OnInventoryUnregistered;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        CurrentStorage = null;
        CurrentInventory = null;
        OnStorageRegistered = null;
        OnStorageUnregistered = null;
        OnInventoryRegistered = null;
        OnInventoryUnregistered = null;
    }
#endif

    public static void RegisterStorage(UI_Storage storage)
    {
        if (storage == null) return;

        CurrentStorage = storage;
        OnStorageRegistered?.Invoke(storage);
    }

    public static void UnregisterStorage(UI_Storage storage)
    {
        if (storage == null || CurrentStorage != storage) return;

        CurrentStorage = null;
        OnStorageUnregistered?.Invoke();
    }

    public static void RegisterInventory(UI_Inventory inventory)
    {
        if (inventory == null) return;

        CurrentInventory = inventory;
        OnInventoryRegistered?.Invoke(inventory);
    }

    public static void UnregisterInventory(UI_Inventory inventory)
    {
        if (inventory == null || CurrentInventory != inventory) return;

        CurrentInventory = null;
        OnInventoryUnregistered?.Invoke();
    }
}
