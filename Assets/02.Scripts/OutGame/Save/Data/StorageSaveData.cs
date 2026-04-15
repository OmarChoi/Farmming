using System;
using System.Collections.Generic;

[Serializable]
public class StorageSaveData
{
    public string BuildingId;
    public int AnchorX;
    public int AnchorY;
    public int AnchorZ;
    public int SlotCount;
    public List<InventorySlotSaveData> Slots = new();
}
