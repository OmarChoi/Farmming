using UnityEngine;

[CreateAssetMenu(fileName = "GatheringObject", menuName = "Gathering/GatheringObjectSO")]
public class GatheringObjectSO : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string _objectName;
    [SerializeField] private int _maxHealth = 100;

    // todo: 곡룡 시스템 구현 후 재작성
    [Header("곡룡 요구사항")]
    // [SerializeField] private EGokryongType _requiredGokryongType;
    // [SerializeField] private int _requireGokryongGrade;

    [Header("드롭 보상")]
    [SerializeField] private DropEntry[] _drops;

    public string ObjectName => _objectName;
    public int MaxHealth => _maxHealth;
    public DropEntry[] Drops => _drops;
}
