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
        TimeEvents.OnNetDayStarted += HandleMorning;
        if (!_owner.IsMine) return;
        OnLocalPlayerReady?.Invoke(this);
    }

    private void OnDestroy()
    {
        TimeEvents.OnNetDayStarted -= HandleMorning;
    }

    public bool TryConsume(float amount) => Stamina.TryConsume(amount);
    public bool HasEnough(float amount) => Stamina.HasEnough(amount);
    public void Consume(float amount) => Stamina.Consume(amount);
    public bool TryRecover(float amount) => Stamina.TryRecover(amount);

    private void HandleMorning() => Stamina.RecoverFull();
}
