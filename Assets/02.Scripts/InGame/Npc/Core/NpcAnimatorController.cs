using System.Collections.Generic;
using UnityEngine;

public class NpcAnimatorController : MonoBehaviour, IMovementAnimator
{
    [Header("애니메이터")]
    [SerializeField] private Animator _animator;

    [Header("애니메이터 매개변수 명칭 (최대한 통일)")]
    [SerializeField] private string _moveFloatName = "Move";
    [SerializeField] private string _greetTriggerName = "Greet";
    [SerializeField] private string _talkTriggerName = "Talk";

    // 애니메이터 해시입니다.
    private int _moveHash;
    private int _greetHash;
    private int _talkHash;

    // 애니메이터 리셋 리스트입니다.
    private readonly List<int> _resetFloatHashes = new();
    private readonly List<int> _resetTriggerHashes = new();

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        _moveHash = Animator.StringToHash(_moveFloatName);
        _greetHash = Animator.StringToHash(_greetTriggerName);
        _talkHash = Animator.StringToHash(_talkTriggerName);

        BuildResetLists();
    }

    private void BuildResetLists()
    {
        _resetTriggerHashes.Clear();
        _resetFloatHashes.Clear();

        _resetFloatHashes.Add(_moveHash);
        _resetTriggerHashes.Add(_greetHash);
        _resetTriggerHashes.Add(_talkHash);
    }

    // ---- 외부 호출용 ----
    // 스폰/리스폰/풀에서 꺼낼 때 호출을 추천합니다.
    public void SetMove(float moveSpeed)
    {
        if (_animator == null) return;
        _animator.SetFloat(_moveHash, moveSpeed);
    }

    public void PlayGreet()
    {
        if (_animator == null) return;

        ResetAll();
        _animator.SetTrigger(_greetHash);
    }

    public void PlayTalk()
    {
        if (_animator == null) return;

        ResetAll();
        _animator.SetTrigger(_talkHash);
    }

    // ---- 내부 호출용 ----
    private void ResetAll()
    {
        if (_animator == null) return;

        // Float를 리셋합니다.
        for (int i = 0; i < _resetFloatHashes.Count; i++)
        {
            _animator.SetFloat(_resetFloatHashes[i], 0);
        }

        // Trigger를 리셋합니다.
        for (int i = 0; i < _resetTriggerHashes.Count; i++)
        {
            _animator.ResetTrigger(_resetTriggerHashes[i]);
        }
    }
}
