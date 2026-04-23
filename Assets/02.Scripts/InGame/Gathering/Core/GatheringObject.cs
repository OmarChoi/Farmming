using System;
using UnityEngine;

public abstract class GatheringObject : MonoBehaviour, IGatherable
{
    [SerializeField] private GatheringObjectSO _gatheringData;
    [SerializeField] private Transform _modelRoot;
    [SerializeField] private int _gatherExperience = 10;

    private int _currentHealth;
    private int _selectedModelIndex = -1;
    private TerrainCell _rootCell;
    private GameObject _modelInstance;

    public static event Action<GatheringObject, PlayerController> OnGatheringCompleted;
    public static event Action<GatheringObject, PlayerController, ItemDataSO, int> OnGatheringItemAdded;
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

        _rootCell = GetComponentInParent<TerrainCell>();
        if (_rootCell == null)
        {
            Debug.LogError("Terrain Cell이 할당되지 않았습니다.");
            return;
        }

        _currentHealth = _gatheringData.MaxHealth;
        SpawnModel();
        Init();
    }

    private void SpawnModel()
    {
        if (_gatheringData == null || _modelRoot == null)
            return;

        GameObject modelPrefab;
        _selectedModelIndex = -1;

        if (_rootCell != null && _gatheringData.ModelCount > 1)
        {
            // Grid position based deterministic selection so every client sees the same model.
            Vector3Int pos = _rootCell.GridPosition;
            int hash = pos.x * 73856093 ^ pos.y * 19349669 ^ pos.z * 83492791;
            _selectedModelIndex = ((hash % _gatheringData.ModelCount) + _gatheringData.ModelCount) % _gatheringData.ModelCount;
            modelPrefab = _gatheringData.GetModelByIndex(_selectedModelIndex);
        }
        else
        {
            if (_gatheringData.ModelCount > 0)
            {
                _selectedModelIndex = UnityEngine.Random.Range(0, _gatheringData.ModelCount);
                modelPrefab = _gatheringData.GetModelByIndex(_selectedModelIndex);
            }
            else
            {
                modelPrefab = _gatheringData.GetRandomModel();
            }
        }

        if (modelPrefab == null)
            return;

        if (_modelInstance != null)
            Destroy(_modelInstance);

        _modelInstance = Instantiate(modelPrefab, _modelRoot);
        _modelInstance.transform.localPosition = Vector3.zero;
        _modelInstance.transform.localRotation = Quaternion.identity;
    }

    protected virtual void Init() { }

    public bool TryGather(GatheringInfo info)
    {
        if (info.HelperGrade.CurrentGrade < _gatheringData.RequiredLevel)
            return false;
        if (_currentHealth < 0)
            return false;

        _currentHealth -= info.Damage;
        Hit();

        if (_currentHealth <= 0)
        {
            OnGatheringCompleted?.Invoke(this, info.Player);
            OnDepleted(info);
        }

        return true;
    }

    protected abstract void Hit();

    protected virtual void OnDepleted(GatheringInfo info)
    {
        var inventory = info.Player.GetAbility<PlayerInventoryAbility>();
        float yieldMultiplier = WorldEffectManager.Instance != null
            ? WorldEffectManager.Instance.GetAcquireMultiplier()
            : 1f;

        AddDrops(inventory, info.Player, _gatheringData.Drops, yieldMultiplier);
        AddDrops(inventory, info.Player, _gatheringData.GetModelOverrideDropsForIndex(_selectedModelIndex), yieldMultiplier);

        info.HelperExperience?.Add(_gatherExperience);
        _rootCell.DestroyObject();
    }

    private void AddDrops(PlayerInventoryAbility inventory, PlayerController player, DropEntry[] drops, float yieldMultiplier)
    {
        if (drops == null || drops.Length == 0)
            return;

        foreach (DropEntry entry in drops)
        {
            int qty = WorldEffectManager.ApplyAcquireMultiplier(entry.GetRandomQuantity(), yieldMultiplier);
            if (entry.Item == null || qty <= 0)
                continue;

            inventory.AddItem(entry.Item, qty);
            OnGatheringItemAdded?.Invoke(this, player, entry.Item, qty);
        }
    }
}
