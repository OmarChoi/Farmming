public class MockCurrencyRepository : ICurrencyRepository
{
    private int _gold;

    public void Save(int gold)
    {
        _gold = gold;
    }

    public int Load()
    {
        return _gold;
    }
}
