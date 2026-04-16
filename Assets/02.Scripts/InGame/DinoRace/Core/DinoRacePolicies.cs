using UnityEngine;

public sealed class DinoRaceEventPolicy
{
    private readonly DinoRaceSettings _settings;

    public DinoRaceEventPolicy(DinoRaceSettings settings)
    {
        _settings = settings ?? new DinoRaceSettings();
    }

    public float GetNextEventDelay()
    {
        return Random.Range(GetRangeMin(_settings.EventIntervalRange), GetRangeMax(_settings.EventIntervalRange));
    }

    public EDinoRaceEventType RollEventType(float speedUpBias = 0f)
    {
        float speedUpWeight = Mathf.Max(0.1f, 1f + Mathf.Clamp(speedUpBias, -1f, 1f) * _settings.SpeedUpPositionBiasWeight);
        float stunWeight = 1f;
        float slowDownWeight = 1f;
        float totalWeight = stunWeight + speedUpWeight + slowDownWeight;
        float roll = Random.Range(0f, totalWeight);

        if (roll < stunWeight)
            return EDinoRaceEventType.Stun;

        roll -= stunWeight;
        if (roll < speedUpWeight)
            return EDinoRaceEventType.SpeedUp;

        return EDinoRaceEventType.SlowDown;
    }

    public float GetEventDuration(EDinoRaceEventType eventType)
    {
        Vector2 range = eventType switch
        {
            EDinoRaceEventType.Stun => _settings.StunDurationRange,
            EDinoRaceEventType.SpeedUp => _settings.SpeedUpDurationRange,
            EDinoRaceEventType.SlowDown => _settings.SlowDownDurationRange,
            _ => Vector2.zero
        };

        return Random.Range(GetRangeMin(range), GetRangeMax(range));
    }

    public float GetSpeedForEvent(EDinoRaceEventType eventType, float baseSpeed)
    {
        return eventType switch
        {
            EDinoRaceEventType.Stun => 0f,
            EDinoRaceEventType.SpeedUp => baseSpeed * Mathf.Max(1f, _settings.SpeedUpMultiplier),
            EDinoRaceEventType.SlowDown => baseSpeed * Mathf.Clamp01(_settings.SlowDownMultiplier),
            _ => baseSpeed
        };
    }

    private static float GetRangeMin(Vector2 range) => Mathf.Min(range.x, range.y);
    private static float GetRangeMax(Vector2 range) => Mathf.Max(range.x, range.y);
}

public sealed class DinoRacePayoutPolicy
{
    private readonly DinoRacePayoutSettings _settings;

    public DinoRacePayoutPolicy(DinoRacePayoutSettings settings)
    {
        _settings = settings ?? new DinoRacePayoutSettings();
    }

    public int CalculatePayout(int betAmount, int finishRank)
    {
        if (betAmount <= 0) return 0;
        return Mathf.Max(0, betAmount * _settings.GetMultiplier(finishRank));
    }
}

public sealed class DinoRaceBetService
{
    private readonly DinoRacePayoutPolicy _payoutPolicy;

    public DinoRaceBetService(DinoRacePayoutPolicy payoutPolicy)
    {
        _payoutPolicy = payoutPolicy ?? new DinoRacePayoutPolicy(new DinoRacePayoutSettings());
    }

    public bool TryPlaceBet(int betAmount, out string error)
    {
        error = null;

        if (betAmount <= 0)
        {
            error = "내기할 금액을 정해주세요!";
            return false;
        }

        if (CurrencyManager.Instance == null)
        {
            error = "Currency system is unavailable.";
            return false;
        }

        if (!CurrencyManager.Instance.CanAfford(betAmount))
        {
            error = "골드가 부족해요!";
            return false;
        }

        if (!CurrencyManager.Instance.TrySpendGold(betAmount))
        {
            error = "베팅에 실패했어요!";
            return false;
        }

        return true;
    }

    public int CalculatePayout(int betAmount, int finishRank)
    {
        return _payoutPolicy.CalculatePayout(betAmount, finishRank);
    }

    public void Pay(int amount)
    {
        if (amount <= 0 || CurrencyManager.Instance == null) return;
        CurrencyManager.Instance.AddGold(amount);
    }
}
