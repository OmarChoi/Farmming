using UnityEngine;

public class PlayerAnimationAbility : PlayerAbility
{
    private static readonly int MoveHash = Animator.StringToHash("Move");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int GreetHash = Animator.StringToHash("Greet");

    private Animator _animator;

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

    public void PlayGreet()
    {
        _animator.SetTrigger(GreetHash);
    }
}