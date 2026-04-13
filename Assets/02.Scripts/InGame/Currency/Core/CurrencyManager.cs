using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    private Currency _gold;

    public event Action<Currency> OnGoldChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _gold = new Currency(0);
    }

    public Currency GetGold()
    {
        return _gold;
    }

    internal void LoadGold(int gold)
    {
        _gold = new Currency(gold);
        OnGoldChanged?.Invoke(_gold);
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        _gold += new Currency(amount);
        OnGoldChanged?.Invoke(_gold);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0) return false;
        if (!CanAfford(amount)) return false;

        _gold -= new Currency(amount);
        OnGoldChanged?.Invoke(_gold);
        return true;
    }

    public bool CanAfford(int amount)
    {
        return _gold >= new Currency(amount);
    }
}
