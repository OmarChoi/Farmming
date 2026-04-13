using UnityEngine;

[CreateAssetMenu(fileName = "TroublemakerSpawnListSO", menuName = "Scriptable Objects/TroublemakerSpawnListSO")]
public class TroublemakerSpawnListSO : ScriptableObject
{
    public TroublemakerSpawnEntry[] Entries;
}
