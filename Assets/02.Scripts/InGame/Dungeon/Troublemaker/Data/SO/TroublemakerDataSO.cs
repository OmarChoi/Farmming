using UnityEngine;

[CreateAssetMenu(fileName = "TroublemakerDataSO", menuName = "Scriptable Objects/Troublemaker/TroublemakerDataSO")]
public class TroublemakerDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string TroublemakerId;
    public string DisplayName;

    [Header("프리팹")]
    public GameObject Prefab;

    [Header("이동")]
    public float WalkSpeed = 3f;
    public float RunSpeed = 6f;
    public float JumpDuration = 0.8f;
    public float JumpHeight = 1.6f;

    [Header("감지")]
    public float DetectRange = 10f;
    public float ChaseRange = 15f;
    public float LoseTargetRange = 20f;

    [Header("행동")]
    public float StopDistance = 1.5f;
    public float RetargetInterval = 0.2f;
}
