using System;

/// 창고 도메인. 고정 크기 슬롯 컨테이너 + 골드 풀.
public class StorageDomain : SlotContainerDomain
{
    public int Gold { get; private set; }

    public event Action<int> OnGoldChanged;

    public StorageDomain(int slotCount) : base(slotCount) { }

    public void SetGold(int amount)
    {
        int clamped = Math.Max(0, amount);
        if (clamped == Gold) return;

        Gold = clamped;
        OnGoldChanged?.Invoke(Gold);
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        SetGold(Gold + amount);
    }

    public bool TryRemoveGold(int amount)
    {
        if (amount <= 0 || Gold < amount) return false;
        SetGold(Gold - amount);
        return true;
    }
}