using UnityEngine;

[CreateAssetMenu(fileName = "GatheringObject", menuName = "Gathering/GatheringObjectSO")]
public class GatheringObjectSO : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string _objectName;
    [SerializeField] private int _maxHealth = 100;

    [Header("나무 등급")]
    [SerializeField] private EHelperGrade _requiredLevel = EHelperGrade.Normal;

    [Header("드롭 보상")]
    [SerializeField] private DropEntry[] _drops;

    public string ObjectName => _objectName;
    public int MaxHealth => _maxHealth;
    public EHelperGrade RequiredLevel => _requiredLevel;
    public DropEntry[] Drops => _drops;
}
