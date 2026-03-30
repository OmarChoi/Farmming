using System;
using UnityEngine;

[Serializable]
public class NpcFriendshipSettings
{
    public int MaxFriendship = 1000;
    public int DefaultFriendship = 0;
    public int AcquaintedThreshold = 200;
    public int InterestedThreshold = 400;
    public int FriendlyThreshold = 600;
    public int TrustedThreshold = 800;
    public int BestThreshold = 950;

    public int HalfStepValue = 50;
    public int HalfStepsPerHeart = 2;

    public int DailyGreetingReward = 20;

    public void Validate()
    {
        MaxFriendship = Mathf.Max(1, MaxFriendship);
        DefaultFriendship = Mathf.Clamp(DefaultFriendship, 0, MaxFriendship);

        AcquaintedThreshold = Mathf.Clamp(AcquaintedThreshold, 0, MaxFriendship);
        InterestedThreshold = Mathf.Clamp(InterestedThreshold, AcquaintedThreshold, MaxFriendship);
        FriendlyThreshold = Mathf.Clamp(FriendlyThreshold, InterestedThreshold, MaxFriendship);
        TrustedThreshold = Mathf.Clamp(TrustedThreshold, FriendlyThreshold, MaxFriendship);
        BestThreshold = Mathf.Clamp(BestThreshold, TrustedThreshold, MaxFriendship);
    }
}
