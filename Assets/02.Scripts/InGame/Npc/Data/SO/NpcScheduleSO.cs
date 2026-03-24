using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NpcScheduleSO", menuName = "Scriptable Objects/Npc/NpcSchedule")]
public class NpcScheduleSO : ScriptableObject
{
    [field: SerializeField] public List<NpcScheduleEntry> ScheduleEntries { get; private set; } = new();
}
