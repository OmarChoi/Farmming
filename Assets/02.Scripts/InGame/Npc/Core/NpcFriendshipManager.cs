using UnityEngine;

public class NpcFriendshipManager : MonoBehaviour
{
    [SerializeField] private NpcFriendshipSettings _friendshipSettings;

    public int MaxFriendship => _friendshipSettings.MaxFriendship;
    public int DefaultFriendship => _friendshipSettings.DefaultFriendship;

    public ENpcFriendshipStep GetFriendshipStep(float friendship)
    {
        if (friendship >= _friendshipSettings.BestThreshold)
        {
            return ENpcFriendshipStep.Best;
        }
        if (friendship >= _friendshipSettings.TrustedThreshold)
        {
            return ENpcFriendshipStep.Trusted;
        }
        if (friendship >= _friendshipSettings.FriendlyThreshold)
        {
            return ENpcFriendshipStep.Friendly;
        }
        if (friendship >= _friendshipSettings.InterestedThreshold)
        {
            return ENpcFriendshipStep.Interested;
        }
        if (friendship >= _friendshipSettings.AcquaintedThreshold)
        {
            return ENpcFriendshipStep.Acquainted;
        }
        return ENpcFriendshipStep.Awkward;
    }
}
