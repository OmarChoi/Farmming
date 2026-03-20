using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Item/ItemSO")]
public class ItemSO : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string _itemName;
    [SerializeField] [TextArea] private string _description;
    [SerializeField] private Sprite _icon;

    public string ItemName => _itemName;
    public string Description => _description;
    public Sprite Icon => _icon;
}
