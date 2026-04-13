using System;
using UnityEngine;

[Serializable]
public class TroublemakerSpawnEntry
{
    [Header("데이터")]
    public TroublemakerDataSO Data;

    [Header("스폰 방식")]
    public ETroublemakerSpawnMode SpawnMode = ETroublemakerSpawnMode.FixedAnchor;
    public string LocationKey;

    [Header("스폰 조건")]
    public bool AlwaysSpawn = true;
    public string RequiredQuestId;

    public string TroublemakerId => Data != null ? Data.TroublemakerId : null;
    public string DisplayName => Data != null ? Data.DisplayName : null;
    public GameObject Prefab => Data != null ? Data.Prefab : null;
}
