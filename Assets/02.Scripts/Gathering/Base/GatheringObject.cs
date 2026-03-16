using System;
using UnityEngine;

public abstract class GatheringObject : MonoBehaviour
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

    // todo: 곡룡 시스템 구현 후 검증 로직 추가
    // public bool ValidateGokryong(IGokryong gokryong)

    public bool TryGather(int damage)
    {
        // todo. 곡룡 검증 로직 확인
        _currentHealth -= damage;
        if (_currentHealth < 0) _currentHealth = 0;
        Hit();

        if (_currentHealth <= 0)
        {
            OnGatheringCompleted?.Invoke(this);
            OnDepleted();
        }

        return true;
    }

    protected abstract void Hit();

    protected virtual void OnDepleted()
    {
        Destroy(gameObject);
    }
}
