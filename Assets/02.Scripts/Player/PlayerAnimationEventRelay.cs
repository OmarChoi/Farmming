using UnityEngine;

public class PlayerAnimationEventRelay : MonoBehaviour
{
    private PlayerController _controller;
    private PlayerAnimationAbility _animationAbility;

    private void Start()
    {
        _controller = GetComponentInParent<PlayerController>();
        _animationAbility = _controller.GetAbility<PlayerAnimationAbility>();
    }

    // Animation Event에서 호출
    public void OnJumpApexEvent()
    {
        _animationAbility.OnJumpApexEvent();
    }
}