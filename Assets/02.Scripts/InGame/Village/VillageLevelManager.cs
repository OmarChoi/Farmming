using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class VillageLevelManager : MonoBehaviourPunCallbacks
{
    public static VillageLevelManager Instance { get; private set; }

    [SerializeField] private VillageLevelRequirementSO _requirementSO;

    public int CurrentLevel { get; private set; } = 1;
    public int CurrentVitality { get; private set; }
    public int CurrentVitalityThreshold => GetVitalityThresholdForLevel(CurrentLevel);
    public LevelUpRequirement LevelUpRequirement => _requirementSO.GetRequirement(CurrentLevel);
    private bool CanLevelUp => !_requirementSO.IsMaxLevel(CurrentLevel)
                               && CurrentVitality >= CurrentVitalityThreshold
                               && BuildingManager.Instance != null
                               && BuildingManager.Instance.BuildingCount >= GetBuildingCountForLevel(CurrentLevel);

    public event Action OnVillageStateChanged;

    private readonly HashSet<string> _builtOnceIds = new HashSet<string>();
    private bool _subscribed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _subscribed = false;
    }

    public override void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    public override void OnDisable()
    {
        if (_subscribed && BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnBuildingBuilt -= HandleBuildingBuilt;
            BuildingManager.Instance.OnBuildingDestroyed -= HandleBuildingDestroyed;
        }
        _subscribed = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;
        if (BuildingManager.Instance == null) return;
        BuildingManager.Instance.OnBuildingBuilt += HandleBuildingBuilt;
        BuildingManager.Instance.OnBuildingDestroyed += HandleBuildingDestroyed;
        _subscribed = true;
    }

    private void HandleBuildingBuilt(BuildingDataSO data)
    {
        if (data == null) return;

        bool isFirst = _builtOnceIds.Add(data.BuildingId);
        if (isFirst && data.FirstBuildGaugeContribution > 0)
        {
            CurrentVitality += data.FirstBuildGaugeContribution;
        }

        if (CanLevelUp)
        {
            RequestLevelUp();
        }
        OnVillageStateChanged?.Invoke();
    }

    private void HandleBuildingDestroyed()
    {
        OnVillageStateChanged?.Invoke();
    }

    public bool IsBuildingUnlocked(BuildingDataSO data) => data != null && CurrentLevel >= data.RequiredVillageLevel;

    private void RequestLevelUp()
    {
        if (!CanLevelUp) return;

        if (!PhotonNetwork.IsConnected)
        {
            CurrentVitality -= CurrentVitalityThreshold;
            CurrentLevel++;
            UIController.Instance.OpenAsync<UI_VillageLevelUp>(ui => ui.SetData(CurrentLevel).Forget());
            OnVillageStateChanged?.Invoke();
            return;
        }

        photonView.RpcSafe(nameof(RPC_RequestLevelUp), RpcTarget.MasterClient);
    }

    [PunRPC]
    private void RPC_RequestLevelUp()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!CanLevelUp) return;

        int threshold = CurrentVitalityThreshold;
        CurrentVitality -= threshold;
        CurrentLevel++;
        UIController.Instance.OpenAsync<UI_VillageLevelUp>(ui => ui.SetData(CurrentLevel).Forget()).Forget();

        photonView.RpcSafe(nameof(RPC_SetVillageLevel), RpcTarget.Others, CurrentLevel, CurrentVitality);
        OnVillageStateChanged?.Invoke();
    }

    [PunRPC]
    private void RPC_SetVillageLevel(int level, int gauge)
    {
        if (PhotonNetwork.IsMasterClient) return;
        CurrentLevel = level;
        CurrentVitality = gauge;
        UIController.Instance.OpenAsync<UI_VillageLevelUp>(ui => ui.SetData(CurrentLevel).Forget()).Forget();
        OnVillageStateChanged?.Invoke();
    }

    private int GetVitalityThresholdForLevel(int level)
    {
        return _requirementSO.GetRequirement(level).VitalityThreshold;
    }

    private int GetBuildingCountForLevel(int level)
    {
        return _requirementSO.GetRequirement(level).BuildingCountThreshold;
    }

    #region Sync
    
    public void ImportSaveData(VillageSaveData saveData)
    {
        CurrentLevel = Math.Max(saveData.Level, 1);
        CurrentVitality = saveData.Gauge;
        foreach (string id in saveData.BuiltBuildings)
        {
            _builtOnceIds.Add(id);
        }
        OnVillageStateChanged?.Invoke();
    }

    public VillageSaveData ExportSaveData()
    {
        return new VillageSaveData
        {
            Level = CurrentLevel,
            Gauge = CurrentVitality,
            BuiltBuildings = _builtOnceIds.ToList()
        };
    }
    #endregion

}