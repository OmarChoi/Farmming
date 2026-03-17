using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SeedDatabase", menuName = "Scriptable Objects/Seed")]
public class SeedDatabase : ScriptableObject
{
    [SerializeField] List<SeedConfig> _seeds;
    private Dictionary<string, SeedConfig> _seedDictionary;

    public SeedConfig GetById(string seedId)
    {
        EnsureDictionary();
        _seedDictionary.TryGetValue(seedId, out SeedConfig seed);
        return seed;
    }

    private void EnsureDictionary()
    {
        if (_seedDictionary != null && _seedDictionary.Count > 0) return;

        _seedDictionary = new Dictionary<string, SeedConfig>();
        foreach (var seed in _seeds)
        {
            if (seed != null && !string.IsNullOrEmpty(seed.SeedId))
            {
                _seedDictionary[seed.SeedId] = seed;
            }
        }
    }
}
