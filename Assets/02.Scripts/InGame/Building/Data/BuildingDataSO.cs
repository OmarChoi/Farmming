using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "Scriptable Objects/Building Data")]
public class BuildingDataSO : ScriptableObject
{
    [SerializeField] private string _buildingId;
    [SerializeField] private string _displayName;
    [SerializeField] private int _width = 2;
    [SerializeField] private int _depth = 2;
    [SerializeField] private int _constructionDays = 0;
    [SerializeField] private Sprite _icon;

    public string BuildingId => _buildingId;
    public string DisplayName => _displayName;
    public int Width => _width;
    public int Depth => _depth;
    public int ConstructionDays => _constructionDays;
    public Sprite Icon => _icon;
}
