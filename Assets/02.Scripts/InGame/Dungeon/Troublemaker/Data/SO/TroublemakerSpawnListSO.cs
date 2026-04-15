using UnityEngine;

[CreateAssetMenu(fileName = "TroublemakerSpawnListSO", menuName = "Scriptable Objects/Troublemaker/TroublemakerSpawnListSO")]
public class TroublemakerSpawnListSO : ScriptableObject
{
    public TroublemakerSpawnEntry[] Entries;
}
