using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NPCSchedule", menuName = "Scriptable Objects/Npc/NpcSchedule")]
public class NpcSchedule : ScriptableObject
{
    public List<NpcScheduleEntry> NpcScheduleEntries;
}
