using System;
using UnityEngine;

public enum EDinoRaceEventType
{
    None = 0,
    Stun = 1,
    SpeedUp = 2,
    SlowDown = 3
}

public enum EDinoRaceState
{
    Idle = 0,
    Countdown = 1,
    Running = 2,
    Finished = 3
}

[Serializable]
public sealed class DinoRacePayoutSettings
{
    [Min(0)] public int FirstPlaceMultiplier = 2;
    [Min(0)] public int SecondPlaceMultiplier = 1;
    [Min(0)] public int ThirdPlaceMultiplier = 0;

    public int GetMultiplier(int finishRank)
    {
        return finishRank switch
        {
            1 => FirstPlaceMultiplier,
            2 => SecondPlaceMultiplier,
            3 => ThirdPlaceMultiplier,
            _ => 0
        };
    }
}

[Serializable]
public sealed class DinoRaceSettings
{
    [Min(0.1f)] public float TrackLength = 18f;
    [Min(0.1f)] public float BaseSpeed = 3f;
    [Min(0f)] public float CountdownDuration = 3f;
    [Min(1)] public int DefaultBetAmount = 100;
    [Min(1)] public int BetStepAmount = 100;
    [Min(1)] public int MinBetAmount = 100;

    public Vector2 EventIntervalRange = new Vector2(2f, 4f);
    public Vector2 StunDurationRange = new Vector2(0.8f, 1.5f);
    public Vector2 SpeedUpDurationRange = new Vector2(1.2f, 2f);
    public Vector2 SlowDownDurationRange = new Vector2(1.2f, 2f);

    [Min(1f)] public float SpeedUpMultiplier = 1.5f;
    [Range(0f, 1f)] public float SlowDownMultiplier = 0.6f;

    public DinoRacePayoutSettings Payout = new DinoRacePayoutSettings();
}

public sealed class DinoRaceSession
{
    public int SelectedRunnerIndex { get; }
    public int BetAmount { get; }

    public DinoRaceSession(int selectedRunnerIndex, int betAmount)
    {
        SelectedRunnerIndex = selectedRunnerIndex;
        BetAmount = betAmount;
    }
}

public readonly struct DinoRaceRunnerSnapshot
{
    public int LaneIndex { get; }
    public string DisplayName { get; }
    public float Distance { get; }
    public float Speed { get; }
    public bool IsFinished { get; }
    public int FinishRank { get; }
    public EDinoRaceEventType EventType { get; }

    public DinoRaceRunnerSnapshot(
        int laneIndex,
        string displayName,
        float distance,
        float speed,
        bool isFinished,
        int finishRank,
        EDinoRaceEventType eventType)
    {
        LaneIndex = laneIndex;
        DisplayName = displayName;
        Distance = distance;
        Speed = speed;
        IsFinished = isFinished;
        FinishRank = finishRank;
        EventType = eventType;
    }
}

public sealed class DinoRaceResult
{
    public DinoRaceSession Session { get; }
    public int SelectedRunnerFinishRank { get; }
    public int PayoutAmount { get; }
    public DinoRaceRunnerSnapshot[] FinalSnapshots { get; }

    public DinoRaceResult(
        DinoRaceSession session,
        int selectedRunnerFinishRank,
        int payoutAmount,
        DinoRaceRunnerSnapshot[] finalSnapshots)
    {
        Session = session;
        SelectedRunnerFinishRank = selectedRunnerFinishRank;
        PayoutAmount = payoutAmount;
        FinalSnapshots = finalSnapshots ?? Array.Empty<DinoRaceRunnerSnapshot>();
    }
}
