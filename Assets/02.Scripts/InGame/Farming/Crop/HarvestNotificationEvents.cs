using System;
using UnityEngine;

public static class HarvestNotificationEvents
{
    public static event Action<Sprite, string, int> Harvested;

    public static void RaiseHarvested(Sprite icon, string itemName, int amount)
    {
        Harvested?.Invoke(icon, itemName, amount);
    }
}
