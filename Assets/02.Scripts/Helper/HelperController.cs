using System;
using System.Collections.Generic;
using UnityEngine;

public class HelperController : MonoBehaviour
{
    public EHelperState State { get; private set; } = EHelperState.Summoned;
    public Transform FollowTarget { get; private set; }

    private readonly Dictionary<Type, HelperAbility> _abilityCache = new();

    public T GetAbility<T>() where T : HelperAbility
    {
        var type = typeof(T);

        if (_abilityCache.TryGetValue(type, out var cached))
            return cached as T;

        var ability = GetComponentInChildren<T>();
        if (ability != null)
            _abilityCache[type] = ability;

        return ability;
    }

    public void Summon(Transform followTarget)
    {
        FollowTarget = followTarget;
        State = EHelperState.Summoned;
        transform.SetParent(null);
        gameObject.SetActive(true);
    }

    public void Equip(Transform equipSlot)
    {
        State = EHelperState.Equipped;
        transform.SetParent(equipSlot);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        GetAbility<HelperAnimationAbility>()?.Play(EHelperAnim.Equipped);
    }

    public void Unequip()
    {
        State = EHelperState.Summoned;
        transform.SetParent(null);
        transform.position = FollowTarget.position + FollowTarget.right * 1.5f;
        GetAbility<HelperAnimationAbility>()?.Play(EHelperAnim.Idle);
    }

    public void Interact(TerrainCell cell)
    {
        var action = GetComponentInChildren<IHelperAction>();
        if (action != null)
            action.Interact(cell);
    }
}