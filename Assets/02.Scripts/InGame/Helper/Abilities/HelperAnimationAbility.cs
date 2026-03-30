using Photon.Pun;
using UnityEngine;

public class HelperAnimationAbility : HelperAbility
{
    private static readonly int AnimHash = Animator.StringToHash("animation");

    private Animator _animator;
    private EHelperAnim _currentAnim;

    protected override void Awake()
    {
        base.Awake();
        _animator = _owner.GetComponentInChildren<Animator>();
    }

    public void Play(EHelperAnim anim)
    {
        if (_currentAnim == anim) return;
        _currentAnim = anim;
        _animator.SetInteger(AnimHash, (int)anim);

        if (_owner.IsMine && _owner.PhotonView != null)
            _owner.PhotonView.RPC(nameof(HelperController.RPC_PlayAnimation), RpcTarget.Others, (int)anim);
    }

    public void PlayLocal(EHelperAnim anim)
    {
        if (_currentAnim == anim) return;
        _currentAnim = anim;
        _animator.SetInteger(AnimHash, (int)anim);
    }
}