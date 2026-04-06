using System;

public interface IFriendshipService
{
    int GetFriendship(string npcId);

    event Action<string, int, int, ENpcFriendshipReason> OnFriendshipChanged;
}
