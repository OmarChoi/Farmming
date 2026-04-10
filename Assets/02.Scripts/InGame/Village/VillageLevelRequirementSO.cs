using System;
using UnityEngine;

[Serializable]
public struct LevelUpRequirement
{
    public int VitalityThreshold;
    public int BuildingCountThreshold;

    public float GetVitalityRatio(int current) =>
        VitalityThreshold > 0 ? (float)current / VitalityThreshold : 1f;

    public float GetBuildingRatio(int current) =>
        BuildingCountThreshold > 0 ? (float)current / BuildingCountThreshold : 1f;
}

[CreateAssetMenu(fileName = "Settings", menuName = "VillageLevelRequirement")]
public class VillageLevelRequirementSO : ScriptableObject
{
    [SerializeField] private int _maxLevel;
    [SerializeField] private LevelUpRequirement[] _requirements;

    public bool IsMaxLevel(int currentLevel) => currentLevel >= _maxLevel;
    public int MaxLevel => _maxLevel;
    public LevelUpRequirement GetRequirement(int currentLevel)
    {
        if (currentLevel >= _maxLevel || _requirements.Length == 0)
        {
            return new LevelUpRequirement
            {
                VitalityThreshold = int.MaxValue,
                BuildingCountThreshold = int.MaxValue
            };
        }
        return currentLevel <= 0 ? _requirements[0] : _requirements[currentLevel - 1];
    }
    
    private void OnValidate()
    {
        if (_maxLevel < 1)
        {
            Debug.LogWarning($"{name}: MaxLevel must be at least 1.", this);
            _maxLevel = 1;
        }

        int expected = Mathf.Max(0, _maxLevel - 1);
        if (_requirements == null || _requirements.Length != expected)
        {
            Debug.LogWarning($"{name}: Requirements length should be {expected} (MaxLevel - 1), but is {_requirements?.Length ?? 0}.", this);
            Array.Resize(ref _requirements, expected);
        }

        for (int i = 0; i < _requirements.Length; i++)
        {
            if (_requirements[i].VitalityThreshold <= 0)
            {
                _requirements[i].VitalityThreshold = 1;
            }
            if (_requirements[i].BuildingCountThreshold <= 0)
            {
                _requirements[i].BuildingCountThreshold = 1;
            }
        }
    }
}