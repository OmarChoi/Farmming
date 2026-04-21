using System;
using UnityEngine;

public static class HarvestNotificationEvents
{
    public static event Action<Sprite, string, int> OnHarvested;

    public static void RaiseHarvested(Sprite icon, string itemName, int amount)
    {
        OnHarvested?.Invoke(icon, itemName, amount);
    }
}
