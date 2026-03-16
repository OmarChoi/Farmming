using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    private ICurrencyRepository _repository;
    private Currency _gold;

    public event Action<Currency> OnGoldChanged;

    private void Awake()
    {
        Instance = this;

        _repository = new MockCurrencyRepository();
        _gold = new Currency(_repository.Load());
    }

    public Currency GetGold()
    {
        return _gold;
    }

    public void AddGold(double amount)
    {
        if (amount <= 0) return;

        _gold += new Currency(amount);
        _repository.Save((double)_gold);
        OnGoldChanged?.Invoke(_gold);
    }

    public bool TrySpendGold(double amount)
    {
        if (amount <= 0) return false;
        if (!CanAfford(amount)) return false;

        _gold -= new Currency(amount);
        _repository.Save((double)_gold);
        OnGoldChanged?.Invoke(_gold);
        return true;
    }

    public bool CanAfford(double amount)
    {
        return _gold >= new Currency(amount);
    }
}
