using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "Scriptable Objects/Building Data")]
public class BuildingDataSO : ScriptableObject
{
    [Header("Building Data")]
    [SerializeField] private string _buildingId;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [TextArea(2, 4)]
    [SerializeField] private string _description;

    [Header("Construction Data")]
    [SerializeField] private int _width = 2;
    [SerializeField] private int _depth = 2;
    [SerializeField] private int _constructionDays = 0;

    [Header("Cost")]
    [SerializeField] private BuildingCostEntry[] _costs = Array.Empty<BuildingCostEntry>();

    [Header("Village Level")]
    [SerializeField] private int _requiredVillageLevel = 1;
    [SerializeField] private int _firstBuildGaugeContribution = 0;

    public string BuildingId => _buildingId;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public string Description => _description;

    public int Width => _width;
    public int Depth => _depth;
    public int ConstructionDays => _constructionDays;
    public IReadOnlyList<BuildingCostEntry> Costs => _costs;

    public int RequiredVillageLevel => _requiredVillageLevel;
    public int FirstBuildGaugeContribution => _firstBuildGaugeContribution;
}
