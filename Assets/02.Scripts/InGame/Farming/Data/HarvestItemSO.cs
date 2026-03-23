using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HarvestItemSO", menuName = "Scriptable Objects/HarvestItemSO")]
public class HarvestItemSO : ScriptableObject
{
    public event Action<Sprite, string, int> OnHarvested;

    public void Raise(Sprite icon, string seedName, int amount)
    {
        OnHarvested?.Invoke(icon, seedName, amount);
    }
}
