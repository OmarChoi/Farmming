using System;
using UnityEngine;

public abstract class HelperVFXAbility : HelperAbility
{
    protected HelperAnimationAbility _animAbility;
    [SerializeField] protected GameObject _effectPrefab;
    [SerializeField] protected Transform _effectSpawnPoint;
    [SerializeField] protected float _effectSeeTime = 2f;

    private void Start()
    {
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
    }

    public abstract void PlayVFX(Vector3 targetPosition);

    protected virtual void PlayEffect(Vector3 targetPosition)
    {
    }
}
