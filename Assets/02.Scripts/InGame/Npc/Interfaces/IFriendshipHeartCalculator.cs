using System.Collections.Generic;

public interface IFriendshipHeartCalculator
{
    int[] CalculateHeartSteps(int friendship, int slotCount);

    List<int> GetChangedHeartSlots(int oldFriendship, int newFriendship, int slotCount);
}
