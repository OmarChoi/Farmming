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
    public int Gold;
    public int InventorySlotCount = 16;
    public int LastCuredDay = -1;
    public List<InventorySlotSaveData> Inventory = new();
    public List<HelperSaveData> Helpers = new();
    public CustomizeSaveData Customize = new();

    public ETutorialState TutorialState = ETutorialState.None;
    public QuestSaveData Quest = new();

    public int LastCheckedDailyQuestDay = -1;     // 일일 퀘스트 초기화 확인용
    public string LastCheckedWorldEffectQuestId;  // 월드 이펙트 퀘스트(신전) 초기화 확인용
}
