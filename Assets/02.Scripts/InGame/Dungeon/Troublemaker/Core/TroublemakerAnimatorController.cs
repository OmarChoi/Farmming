using UnityEngine;

public class TroublemakerAnimatorController : MonoBehaviour, IMovementAnimator
{
    [Header("애니메이터")]
    [SerializeField] private Animator _animator;

    [Header("애니메이터 매개변수 명칭 (최대한 통일)")]
    [SerializeField] private string _moveFloatName = "Move";
    [SerializeField] private string _detectTriggerName = "Detect";
    [SerializeField] private string _troubleTriggerName = "Trouble";

    [Header("감지 애니메이션 길이")]
    [SerializeField] private float _detectDuration = 0.6f;

    private int _moveHash;
    private int _detectHash;
    private int _troubleHash;

    public float DetectDuration => _detectDuration;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        _moveHash = Animator.StringToHash(_moveFloatName);
        _detectHash = Animator.StringToHash(_detectTriggerName);
        _troubleHash = Animator.StringToHash(_troubleTriggerName);
    }

    public void SetMove(float value)
    {
        if (_animator == null) return;
        _animator.SetFloat(_moveHash, value);
    }

    public void PlayDetect()
    {
        if (_animator == null) return;
        _animator.SetTrigger(_detectHash);
    }

    public void PlayTrouble()
    {
        if (_animator == null) return;
        _animator.SetTrigger(_troubleHash);
    }
}
