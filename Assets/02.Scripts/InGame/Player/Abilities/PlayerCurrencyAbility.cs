public class PlayerCurrencyAbility : PlayerAbility, ISaveableAbility
{
    public void ExportTo(PlayerSaveData saveData)
    {
        if (saveData == null) return;
        if (_owner == null || !_owner.IsMine) return;
        if (CurrencyManager.Instance == null) return;

        saveData.Gold = (int)CurrencyManager.Instance.GetGold();
    }

    public void ImportFrom(PlayerSaveData saveData)
    {
        if (saveData == null) return;
        if (_owner == null || !_owner.IsMine) return;
        if (CurrencyManager.Instance == null) return;

        CurrencyManager.Instance.LoadGold(saveData.Gold);
    }
}
