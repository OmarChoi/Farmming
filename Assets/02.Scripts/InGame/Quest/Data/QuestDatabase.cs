using UnityEngine;
using System.Collections.Generic;

public class QuestDatabase : MonoBehaviour
{
    [SerializeField] private List<QuestDataSO> _allQuests = new();

    private Dictionary<string, QuestDataSO> _questMap;

    private void Awake()
    {
        _questMap = new Dictionary<string, QuestDataSO>();

        foreach (QuestDataSO quest in _allQuests)
        {
            if (quest == null || string.IsNullOrEmpty(quest.QuestId)) continue;
            _questMap[quest.QuestId] = quest;
        }
    }

    public QuestDataSO GetQuestById(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return null;
        return _questMap.TryGetValue(questId, out QuestDataSO quest) ? quest : null;
    }
}
