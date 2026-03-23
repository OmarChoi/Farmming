using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestBoardDataSO", menuName = "Scriptable Objects/Quest/QuestBoardDataSO")]
public class QuestBoardDataSO : ScriptableObject
{
    public List<QuestDataSO> AllQuests;
}
