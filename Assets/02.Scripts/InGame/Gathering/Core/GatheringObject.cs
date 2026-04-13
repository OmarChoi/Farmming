using System;
using UnityEngine;

public abstract class GatheringObject : MonoBehaviour, IGatherable
{
    [SerializeField] private GatheringObjectSO _gatheringData;
    [SerializeField] private Transform _modelRoot;
    [SerializeField] private int _gatherExperience = 10;

    private int _currentHealth;
    private TerrainCell _rootCell;
    private GameObject _modelInstance;

    public static event Action<GatheringObject, PlayerController> OnGatheringCompleted;
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
        if (_gatheringData == null || _modelRoot == null) return;

        GameObject modelPrefab;
        if (_rootCell != null && _gatheringData.ModelCount > 1)
        {
            // 그리드 좌표 기반 결정적 선택 — 모든 클라이언트가 동일한 모델을 봄
            var pos = _rootCell.GridPosition;
            int hash = pos.x * 73856093 ^ pos.y * 19349669 ^ pos.z * 83492791;
            int index = ((hash % _gatheringData.ModelCount) + _gatheringData.ModelCount) % _gatheringData.ModelCount;
            modelPrefab = _gatheringData.GetModelByIndex(index);
        }
        else
        {
            modelPrefab = _gatheringData.GetRandomModel();
        }

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
        foreach (DropEntry entry in _gatheringData.Drops)
        {
            int qty = WorldEffectManager.ApplyMultiplier(entry.GetRandomQuantity(), yieldMultiplier);
            QuestReportItemHelper.AddItemAndReportQuest(inventory, entry.Item, qty);
        }

        info.HelperExperience?.Add(_gatherExperience);
        _rootCell.DestroyObject();
    }
}
