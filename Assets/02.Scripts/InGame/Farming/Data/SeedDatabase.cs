using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SeedDatabase", menuName = "Scriptable Objects/Seed")]
public class SeedDatabase : ScriptableObject
{
    [SerializeField] private List<SeedItemDataSO> _seeds;
    private Dictionary<int, SeedItemDataSO> _seedDictionary;

    public SeedItemDataSO GetById(int seedId)
    {
        EnsureDictionary();
        _seedDictionary.TryGetValue(seedId, out SeedItemDataSO seed);
        return seed;
    }

    private void EnsureDictionary()
    {
        if (_seedDictionary != null && _seedDictionary.Count > 0) return;

        _seedDictionary = new Dictionary<int, SeedItemDataSO>();
        foreach (var seed in _seeds)
        {
            if (seed != null && seed.Id > 0)
            {
                _seedDictionary[seed.Id] = seed;
            }
        }
    }
}