using UnityEngine;
using System.Collections.Generic;

public class NpcQuest : MonoBehaviour
{
    [SerializeField] private List<QuestDataSO> _quests = new();

    public IReadOnlyList<QuestDataSO> Quests => _quests;
}
