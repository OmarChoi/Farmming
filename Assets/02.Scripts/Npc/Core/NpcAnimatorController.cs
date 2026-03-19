using System.Collections.Generic;
using UnityEngine;

public class NpcAnimatorController : MonoBehaviour
{
    [Header("애니메이터")]
    [SerializeField] private Animator _animator;

    [Header("애니메이터 매개변수 명칭 (최대한 통일)")]
    [SerializeField] private string _moveBoolName = "Move";
    [SerializeField] private string _greetTriggerName = "Greet";

    // 애니메이터 해시입니다.
    private int _moveHash;
    private int _greetHash;

    // 애니메이터 리셋 리스트입니다.
    private readonly List<int> _resetTriggerHashes = new();
    private readonly List<int> _resetBoolHashes = new();

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        _moveHash = Animator.StringToHash(_moveBoolName);
        _greetHash = Animator.StringToHash(_greetTriggerName);

        BuildResetLists();
    }

    private void BuildResetLists()
    {
        _resetTriggerHashes.Clear();
        _resetBoolHashes.Clear();

        _resetBoolHashes.Add(_moveHash);
        _resetTriggerHashes.Add(_greetHash);
    }

    // ---- 외부 호출용 ----
    // 스폰/리스폰/풀에서 꺼낼 때 호출을 추천합니다.
    public void SetMove(bool isMoving)
    {
        if (_animator == null) return;
        _animator.SetBool(_moveHash, isMoving);
    }

    public void PlayGreet()
    {
        if (_animator == null) return;

        ResetAll();
        _animator.SetTrigger(_greetHash);
    }

    // ---- 내부 호출용 ----
    private void ResetAll()
    {
        if (_animator == null) return;

        // Bool을 리셋합니다.
        for (int i = 0; i < _resetBoolHashes.Count; i++)
        {
            _animator.SetBool(_resetBoolHashes[i], false);
        }

        // Trigger를 리셋합니다.
        for (int i = 0; i < _resetTriggerHashes.Count; i++)
        {
            _animator.ResetTrigger(_resetTriggerHashes[i]);
        }
    }
}
