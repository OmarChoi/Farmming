using UnityEngine;

public class PlayerAnimationAbility : PlayerAbility
{
    private static readonly int MoveHash = Animator.StringToHash("Move");

    private Animator _animator;

    private void Start()
    {
        _animator = _owner.GetComponentInChildren<Animator>();
    }

    public void SetMove(float value)
    {
        _animator.SetFloat(MoveHash, value);
    }
}