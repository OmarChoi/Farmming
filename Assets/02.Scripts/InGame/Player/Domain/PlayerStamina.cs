using System;

public class PlayerStamina
{
    private float _max;

    private float _current;
    public float Current => _current;
    public float Max => _max;

    public event Action<float> OnMaxChanged;
    public bool IsExhausted => Current <= 0f;

    public event Action OnExhausted;
    public event Action OnRecovered;
    public event Action<float> OnChanged;

    public PlayerStamina(float max)
    {
        _max = max;
        _current = max;
    }

    public bool TryConsume(float amount)
    {
        if (Current < amount) return false;

        _current -= amount;
        OnChanged?.Invoke(Current);

        if (Current <= 0f)
        {
            _current = 0f;
            OnExhausted?.Invoke();
        }

        return true;
    }

    public void SetMax(float newMax)
    {
        _max = newMax;
        if (_current > _max) _current = _max;
        OnMaxChanged?.Invoke(_max);
        OnChanged?.Invoke(Current);
    }

    public void RecoverFull()
    {
        _current = _max;
        OnChanged?.Invoke(Current);
        OnRecovered?.Invoke();
    }
}