using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldEffectDatabase", menuName = "Scriptable Objects/World Effect Database")]
public class WorldEffectDatabase : ScriptableObject
{
    [SerializeField] private List<WorldEffectDataSO> _effects;
    private Dictionary<string, WorldEffectDataSO> _effectDictionary;

    public IReadOnlyList<WorldEffectDataSO> Effects => _effects;

    private void OnEnable()
    {
        BuildDictionary();
    }

    public WorldEffectDataSO GetById(string effectId)
    {
        if (_effectDictionary == null) BuildDictionary();
        if (string.IsNullOrEmpty(effectId)) return null;
        _effectDictionary.TryGetValue(effectId, out WorldEffectDataSO data);
        return data;
    }

    private void BuildDictionary()
    {
        _effectDictionary = new Dictionary<string, WorldEffectDataSO>();
        if (_effects == null) return;
        foreach (WorldEffectDataSO effect in _effects)
        {
            if (effect != null && !string.IsNullOrEmpty(effect.EffectId))
            {
                _effectDictionary[effect.EffectId] = effect;
            }
        }
    }
}
