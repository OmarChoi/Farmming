using System;
using UnityEngine;

public abstract class GatheringObject : MonoBehaviour, IGatherable
{
    [SerializeField] private GatheringObjectSO _gatheringData;

    private int _currentHealth;

    public static event Action<GatheringObject> OnGatheringCompleted;
    public GatheringObjectSO GatheringData => _gatheringData;

    protected virtual void Awake()
    {
        _currentHealth = _gatheringData.MaxHealth;
        Init();
    }

    protected virtual void Init() { }

    public bool TryGather(GatheringInfo info)
    {
        if (info.HelperGrade.CurrentGrade > _gatheringData.RequiredLevel) return false;
        if (_currentHealth < 0) return false;
        _currentHealth -= info.Damage;
        Hit();

        if (_currentHealth <= 0)
        {
            OnGatheringCompleted?.Invoke(this);
            OnDepleted(info);
        }

        return true;
    }

    protected abstract void Hit();
    protected virtual void OnDepleted(GatheringInfo info)
    {
        var inventory = info.Player.GetAbility<PlayerInventoryAbility>();
        foreach (DropEntry entry in _gatheringData.Drops)
        {
            inventory.AddItem(entry.Item, entry.GetRandomQuantity());
        }
        Destroy(gameObject);
    }
}
