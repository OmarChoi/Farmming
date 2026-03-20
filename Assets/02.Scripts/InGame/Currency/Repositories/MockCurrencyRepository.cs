public class MockCurrencyRepository : ICurrencyRepository
{
    private double _gold;

    public void Save(double gold)
    {
        _gold = gold;
    }

    public double Load()
    {
        return _gold;
    }
}
