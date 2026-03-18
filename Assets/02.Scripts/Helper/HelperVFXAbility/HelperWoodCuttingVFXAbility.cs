using UnityEngine;

public class HelperWoodCuttingVFXAbility : HelperVFXAbility
{

    public override void PlayVFX(Vector3 targetPosition)
    {
        _animAbility.Play(EHelperAnim.WoodCutting);
        PlayEffect(targetPosition);
    }

}
