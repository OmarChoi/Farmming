using UnityEngine;

[CreateAssetMenu(fileName = "HelperData", menuName = "Data/HelperData")]
public class HelperDataSO : ScriptableObject
{
    [SerializeField] private string _helperName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private HelperController _prefab;

    public string HelperName => _helperName;
    public Sprite Icon => _icon;
    public HelperController Prefab => _prefab;
}