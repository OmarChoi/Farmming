using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingDatabase", menuName = "Scriptable Objects/Building Database")]
public class BuildingDatabase : ScriptableObject
{
    [SerializeField] private List<BuildingDataSO> _buildings;
    private Dictionary<string, BuildingDataSO> _buildingDictionary;

    private void OnEnable()
    {
        BuildDictionary();
    }

    public BuildingDataSO GetById(string buildingId)
    {
        if (_buildingDictionary == null) BuildDictionary();
        _buildingDictionary.TryGetValue(buildingId, out BuildingDataSO building);
        return building;
    }

    private void BuildDictionary()
    {
        _buildingDictionary = new Dictionary<string, BuildingDataSO>();
        if (_buildings == null) return;
        foreach (var building in _buildings)
        {
            if (building != null && !string.IsNullOrEmpty(building.BuildingId))
            {
                _buildingDictionary[building.BuildingId] = building;
            }
        }
    }
}
