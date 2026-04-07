using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Chest Loot Table")]
public class ChestLootTable : ScriptableObject
{
    public ChestLootEntry[] Entries;
}

[Serializable]
public class ChestLootEntry
{
    public ItemDataSO Item;
    public int Amount = 1;
}