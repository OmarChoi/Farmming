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
        _animator = GetComponentInChildren<Animator>();
    }

    public void Play(EHelperAnim anim)
    {
        if (TrySetAnimation(anim) && _owner.IsMine)
            _owner.PhotonView.RpcSafe(nameof(RPC_PlayAnimation), RpcTarget.Others, (int)anim);
    }

    public void PlayLocal(EHelperAnim anim)
    {
        TrySetAnimation(anim);
    }

    [PunRPC]
    internal void RPC_PlayAnimation(int anim)
    {
        PlayLocal((EHelperAnim)anim);
    }

    private bool TrySetAnimation(EHelperAnim anim)
    {
        if (_currentAnim == anim) return false;

        _currentAnim = anim;
        _animator.SetInteger(AnimHash, (int)anim);
        return true;
    }
}