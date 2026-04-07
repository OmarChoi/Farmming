using UnityEngine;

public class PlayerVFXEventRelay : MonoBehaviour
{
    private PlayerVFXAbility _vfx;

    private void Awake()
    {
        _vfx = GetComponentInChildren<PlayerVFXAbility>();
    }

    // Animation Event
    public void OnLeftFootstep()
    {
        if (_vfx != null)
        {
            _vfx.PlayFootstepDust(true);
        }
    }

    // Animation Event
    public void OnRightFootstep()
    {
        if (_vfx != null)
        {
            _vfx.PlayFootstepDust(false);
        }
    }
}