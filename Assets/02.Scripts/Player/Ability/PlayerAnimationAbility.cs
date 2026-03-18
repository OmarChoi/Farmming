using System;
using UnityEngine;

public class PlayerAnimationAbility : PlayerAbility
{
    private static readonly int MoveHash = Animator.StringToHash("Move");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private Animator _animator;

    public event Action OnJumpApex;

    private void Start()
    {
        _animator = _owner.GetComponentInChildren<Animator>();
    }

    public void SetMove(float value)
    {
        _animator.SetFloat(MoveHash, value);
    }

    public void SetGrounded(bool grounded)
    {
        _animator.SetBool(GroundedHash, grounded);
    }

    public void TriggerJump()
    {
        _animator.SetTrigger(JumpHash);
    }

    // Animation Event에서 호출
    public void OnJumpApexEvent()
    {
        OnJumpApex?.Invoke();
    }
}