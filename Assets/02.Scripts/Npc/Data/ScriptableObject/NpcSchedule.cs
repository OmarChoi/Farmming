using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NpcSchedule", menuName = "Scriptable Objects/Npc/NpcSchedule")]
public class NpcSchedule : ScriptableObject
{
    [field: SerializeField] public List<NpcScheduleEntry> ScheduleEntries { get; private set; } = new();
}
