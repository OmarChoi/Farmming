using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SeedDatabase", menuName = "Scriptable Objects/Seed")]
public class SeedDatabase : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField] List<SeedConfig> _seeds;
    private readonly Dictionary<string, SeedConfig> _seedDictionary = new();

    public SeedConfig GetById(string seedId)
    {
        _seedDictionary.TryGetValue(seedId, out SeedConfig seed);
        return seed;
    }

    public void OnAfterDeserialize()
    {
        _seedDictionary.Clear();
        foreach (var seed in _seeds)
        {
            if(seed != null && !string.IsNullOrEmpty(seed.SeedId))
            {
                _seedDictionary[seed.SeedId] = seed;
            }
        }
    }

    public void OnBeforeSerialize() { }
}
