using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NpcSchedule", menuName = "Scriptable Objects/Npc/NpcSchedule")]
public class NpcSchedule : ScriptableObject
{
    public List<NpcScheduleEntry> ScheduleEntries = new();
}
