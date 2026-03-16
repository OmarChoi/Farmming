using UnityEngine;

public enum EItemType
{
    Wood,
    Dirt,
    Stone,
    Crop,
    Seed,
}

[CreateAssetMenu(fileName = "ItemData", menuName = "Data/ItemData")]
public class ItemDataSO : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private int _id;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private EItemType _type;

    [Header("스택")]
    [SerializeField] private int _maxStack = 99;

    public int Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public EItemType Type => _type;
    public int MaxStack => _maxStack;
}