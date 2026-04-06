using Photon.Pun;
using UnityEngine;

/// Move(float), Grounded(bool) → PhotonAnimatorView (자동 연속 동기화)
/// Greet 등 트리거 → RPC (간헐적 이벤트)
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
        if (_animator == null) return;
        _animator.SetFloat(MoveHash, value);
    }

    public void SetGrounded(bool grounded)
    {
        if (_animator == null) return;
        _animator.SetBool(GroundedHash, grounded);
    }

    public void PlayGreet()
    {
        PlayTriggerSynced(GreetHash, "Greet");
    }

    public void PlayTrigger(string triggerName)
    {
        if (_animator != null)
            _animator.SetTrigger(Animator.StringToHash(triggerName));
    }

    private void PlayTriggerSynced(int hash, string name)
    {
        if (_animator == null) return;
        _animator.SetTrigger(hash);

        if (_owner.IsMine)
            _owner.PhotonView.RPC(nameof(PlayerController.RPC_AnimTrigger), RpcTarget.Others, name);
    }
}