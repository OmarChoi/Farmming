using System;
using UnityEngine;

public class HelperEnergy : MonoBehaviour
{
    private HelperDataSO _data;

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
        if(!IsExhausted)
        {
            return;
        }

        Current += _data.EnergyRecoveryPerSecond * deltaTime;
        Current = Mathf.Min(Current, Max);

        if(Current >= Max)
        {
            OnRecovered?.Invoke();
        }
    }

    public void RecoverFull()
    {
        Current = Max;
        OnRecovered?.Invoke();
    }
}
