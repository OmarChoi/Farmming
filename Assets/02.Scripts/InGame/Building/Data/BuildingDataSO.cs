using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "Scriptable Objects/Building Data")]
public class BuildingDataSO : ScriptableObject
{
    [Header("Building Data")]
    [SerializeField] private string _buildingId;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    
    [Header("Construction Data")]
    [SerializeField] private int _width = 2;
    [SerializeField] private int _depth = 2;
    [SerializeField] private int _constructionDays = 0;
    
    [Header("Npc Data")]
    [SerializeField] private NpcDataSO _npcDataData;

    public string BuildingId => _buildingId;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    
    public int Width => _width;
    public int Depth => _depth;
    public int ConstructionDays => _constructionDays;
    
    public NpcDataSO RelatedNpcData => _npcDataData;
}
