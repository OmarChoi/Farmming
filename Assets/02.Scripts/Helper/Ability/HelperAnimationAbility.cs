using UnityEngine;

public class HelperAnimationAbility : HelperAbility
{
    private static readonly int AnimHash = Animator.StringToHash("animation");

    private Animator _animator;
    private EHelperAnim _currentAnim;

    private void Start()
    {
        _animator = _owner.GetComponentInChildren<Animator>();
    }

    public void Play(EHelperAnim anim)
    {
        if (_currentAnim == anim) return;
        _currentAnim = anim;
        _animator.SetInteger(AnimHash, (int)anim);
    }
}