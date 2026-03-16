using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerStatSO _statSo;
    public PlayerStatSO StatSo => _statSo;
    public bool IsUIOpen { get; private set; }

    private readonly Dictionary<Type, PlayerAbility> _abilityCache = new();

    public T GetAbility<T>() where T : PlayerAbility
    {
        var type = typeof(T);

        if (_abilityCache.TryGetValue(type, out var cached))
            return cached as T;

        var ability = GetComponentInChildren<T>();
        if (ability != null)
            _abilityCache[type] = ability;

        return ability;
    }

    public void EnterUIMode()
    {
        IsUIOpen = true;
    }

    public void ExitUIMode()
    {
        IsUIOpen = false;
    }
}