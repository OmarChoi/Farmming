using System;

[Serializable]
public class NpcFriendshipSettings
{
    public int MaxFriendship = 100;
    public int DefaultFriendship = 0;
    public int AcquaintedThreshold = 20;
    public int InterestedThreshold = 40;
    public int FriendlyThreshold = 60;
    public int TrustedThreshold = 80;
    public int BestThreshold = 95;
}
