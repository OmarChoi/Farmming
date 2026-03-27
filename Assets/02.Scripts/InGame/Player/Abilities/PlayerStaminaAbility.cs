using System;
using UnityEngine;

public class PlayerStaminaAbility : PlayerAbility
{
    public static event Action<PlayerStaminaAbility> OnLocalPlayerReady;

    public PlayerStamina Stamina { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Stamina = new PlayerStamina(_owner.StatSo.MaxStamina);
    }

    private void Start()
    {
        if (TimeSystem.Instance != null)
        {
            TimeSystem.Instance.OnDayStarted += HandleMorning;
        }
        OnLocalPlayerReady?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (TimeSystem.Instance != null)
        {
            TimeSystem.Instance.OnDayStarted -= HandleMorning;
        }
    }

    public bool TryConsume(float amount) => Stamina.TryConsume(amount);

    private void HandleMorning() => Stamina.RecoverFull();
}