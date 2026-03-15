using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SeedDatabase", menuName = "Scriptable Objects/Seed")]
public class SeedDatabase : ScriptableObject
{
    public List<SeedConfig> Seeds;

    public SeedConfig GetById(string seedId)
    {
        return Seeds.Find(s => s.SeedId == seedId);
    }
}
