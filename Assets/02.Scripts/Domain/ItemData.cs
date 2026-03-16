using UnityEngine;

public enum ItemType
{
    Tool,
    Seed,
    Crop,
    Material,
    Furniture
}

[CreateAssetMenu(fileName = "ItemData", menuName = "Data/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private ItemType _type;

    [Header("스택")]
    [SerializeField] private int _maxStack = 99;

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public ItemType Type => _type;
    public int MaxStack => _maxStack;
}