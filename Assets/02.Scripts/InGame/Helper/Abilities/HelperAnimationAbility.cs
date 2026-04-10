using Photon.Pun;
using UnityEngine;
using System.Collections;

public class HelperAnimationAbility : HelperAbility
{

    private const float _animWaitTimeout = 3f;
    private const float _forceReplayTimeout = 5f;
    private float _maxTransitionWaitTime = 1f;

    private static readonly int AnimHash = Animator.StringToHash("animation");

    private Animator _animator;
    private EHelperAnim _currentAnim;
    public Animator Animator => _animator;

    protected override void Awake()
    {
        base.Awake();
        _animator = GetComponentInChildren<Animator>();
    }

    public void InitAnimator()
    {
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

    // 현재 재생 중인 애니메이션과 같더라도 처음부터 다시 재생
    public void Replay(EHelperAnim anim)
    {
        _currentAnim = (EHelperAnim)(-1);
        Play(anim);
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

    public IEnumerator WaitForNormalizedTime(float normalizedThreshold, float timeout = _animWaitTimeout, int layerIndex = 0)
    {
        yield return null;

        float elapsed = 0f;
        while (elapsed < timeout)
        {
            if (_animator == null) yield break;
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(layerIndex);
            if (stateInfo.normalizedTime >= normalizedThreshold)
                yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public IEnumerator ForceReplayAndWait(EHelperAnim anim, float normalizedThreshold, float timeout = _forceReplayTimeout, int layerIndex = 0)
    {
        if (_animator == null) yield break;

        _currentAnim = anim;
        _animator.SetInteger(AnimHash, (int)anim);

        if (_owner.IsMine)
            _owner.PhotonView.RpcSafe(nameof(RPC_PlayAnimation), RpcTarget.Others, (int)anim);

        // Let the Animator process the parameter change before rewinding the state.
        // Without this, we can accidentally restart the previous state (for example Idle)
        // and freeze the helper with the wrong pose.
        yield return null;

        float transWait = 0f;
        while (_animator.IsInTransition(layerIndex) && transWait < _maxTransitionWaitTime)
        {
            transWait += Time.deltaTime;
            yield return null;
        }

        // 현재 상태를 normalizedTime=0부터 강제 재시작
        int stateHash = _animator.GetCurrentAnimatorStateInfo(layerIndex).fullPathHash;
        _animator.Play(stateHash, layerIndex, 0f);

        yield return null;

        float elapsed = 0f;
        while (elapsed < timeout)
        {
            if (_animator == null) yield break;
            if (_animator.GetCurrentAnimatorStateInfo(layerIndex).normalizedTime >= normalizedThreshold)
                yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
