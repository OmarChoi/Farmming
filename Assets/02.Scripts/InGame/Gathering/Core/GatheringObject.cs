using System;
using UnityEngine;

public abstract class GatheringObject : MonoBehaviour, IGatherable
{
    [SerializeField] private GatheringObjectSO _gatheringData;
    [SerializeField] private Transform _modelRoot;
    [SerializeField] private int _gatherExperience = 10;

    private int _currentHealth;
    private GameObject _modelInstance;

    public static event Action<GatheringObject> OnGatheringCompleted;
    public GatheringObjectSO GatheringData => _gatheringData;

    protected virtual void Awake()
    {
        ApplyGatheringData();
    }

    public void Setup(GatheringObjectSO data)
    {
        _gatheringData = data;
        ApplyGatheringData();
    }

    private void ApplyGatheringData()
    {
        if (_gatheringData == null)
        {
            Debug.LogWarning("GatheringData가 할당되지 않았습니다.");
            return;
        }

        _currentHealth = _gatheringData.MaxHealth;
        SpawnModel();
        Init();
    }

    private void SpawnModel()
    {
        if (_gatheringData == null || _modelRoot == null) return;

        var modelPrefab = _gatheringData.GetRandomModel();
        if (modelPrefab == null) return;

        if (_modelInstance != null)
            Destroy(_modelInstance);

        _modelInstance = Instantiate(modelPrefab, _modelRoot);
        _modelInstance.transform.localPosition = Vector3.zero;
        _modelInstance.transform.localRotation = Quaternion.identity;
    }

    protected virtual void Init() { }

    public bool TryGather(GatheringInfo info)
    {
        if (info.HelperGrade.CurrentGrade < _gatheringData.RequiredLevel) return false;
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

        info.HelperExperience?.Add(_gatherExperience);
        Destroy(gameObject);
    }
}
