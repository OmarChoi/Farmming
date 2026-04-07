using System;

[Serializable]
public class DungeonNpcSpawnEntry
{
    public NpcDataSO NpcData;
    public bool AlwaysSpawn = true;
    public string RequiredQuestId;
    public string LocationKey;
}
