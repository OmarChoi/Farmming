using UnityEngine;

[CreateAssetMenu(fileName = "DungeonNpcSpawnListSO", menuName = "Scriptable Objects/Npc/DungeonNpcSpawnListSO")]
public class DungeonNpcSpawnListSO : ScriptableObject
{
    public DungeonNpcSpawnEntry[] Entries;
}
