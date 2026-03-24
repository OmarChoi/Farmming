using System;
using System.Collections.Generic;

[Serializable]
public class PlayerSaveData
{
    public string PlayerId;
    public float PosX;
    public float PosY;
    public float PosZ;
    public float RotY;
    public int InventorySlotCount = 16;
    public List<InventorySlotSaveData> Inventory = new();
    public List<HelperSaveData> Helpers = new();
    public CustomizeSaveData Customize = new();
}