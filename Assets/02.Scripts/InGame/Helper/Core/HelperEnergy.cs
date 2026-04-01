using System;
using UnityEngine;

public class HelperEnergy
{
    private readonly HelperDataSO _data;

    public float Current { get; private set; }
    public float Max => _data.MaxEnergy;

    public bool IsExhausted => Current <= 0f;

    public event Action OnExhausted;
    public event Action OnRecovered;

    public HelperEnergy(HelperDataSO data)
    {
        _data = data;
        Current = data.MaxEnergy;
    }

    public bool TryConsume(float amount)
    {
        if(Current < amount)
        {
            return false;
        }

        Current -= amount;

        if(Current <= 0)
        {
            Current = 0;
            OnExhausted?.Invoke();
        }

        return true;
    }

    public void Recover(float deltaTime)
    {
        if(Current >= Max)
        {
            return;
        }

        float previousCurrent = Current;
        Current += _data.EnergyRecoveryPerSecond * deltaTime;
        Current = Mathf.Min(Current, Max);

        if(previousCurrent < Max && Current >= Max)
        {
            OnRecovered?.Invoke();
        }
    }

    public void RecoverFull()
    {
        Current = Max;
        OnRecovered?.Invoke();
    }

    // savedEnergy < 0 이면 최대치로 초기화
    // elapsedSeconds: 소환 해제 후 경과 시간 → 그 동안의 자동회복 반영
    public void Load(float savedEnergy, float elapsedSeconds = 0f)
    {
        if (savedEnergy < 0f)
        {
            Current = Max;
            return;
        }

        Current = Mathf.Min(savedEnergy + _data.EnergyRecoveryPerSecond * elapsedSeconds, Max);
    }
}
