using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Seed", menuName = "Scriptable Objects/Seed")]
public class Seed : ScriptableObject
{
    public List<SeedConfig> Seeds;

    public SeedConfig GetById(string seedId)
    {
        return Seeds.Find(s => s.SeedId == seedId);
    }
}
