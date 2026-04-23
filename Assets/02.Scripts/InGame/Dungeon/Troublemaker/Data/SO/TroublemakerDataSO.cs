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
    public float RunSpeed = 5f;
    public float JumpDuration = 0.8f;
    public float JumpHeight = 1.6f;

    [Header("플레이어 감지")]
    public float DetectRange = 10f;
    public float LoseTargetRange = 15f;

    [Header("행동 관련")]
    public float StopDistance = 1.5f;
    public float RetargetInterval = 0.2f;

    [Header("방해 옵션")]
    public ETroubleEffectType TroubleEffectType = ETroubleEffectType.None;
    public float TroublePower = 2f;
    public float TroubleDuration = 0.5f;
    public float TroubleCooldown = 2.5f;
    public float TroubleHitDelay = 0.25f;
    public float TroubleRange = 4f;

    [Header("사운드 옵션")]
    [SerializeField] private string _detectSfxKey;
    [SerializeField] private string _troubleSfxKey;
    public float SfxVolume = 1f;
    public float SfxPitchMin = 0.95f;
    public float SfxPitchMax = 1.05f;


    public string DetectSfxKey => _detectSfxKey;
    public string TroubleSfxKey => _troubleSfxKey;
}
