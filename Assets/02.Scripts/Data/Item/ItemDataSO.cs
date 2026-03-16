using UnityEngine;

public enum EItemType
{
    Wood,
    Dirt,
    Stone,
}

[CreateAssetMenu(fileName = "ItemData", menuName = "Data/ItemData")]
public class ItemDataSO : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private EItemType _type;

    [Header("스택")]
    [SerializeField] private int _maxStack = 99;

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public EItemType Type => _type;
    public int MaxStack => _maxStack;
}