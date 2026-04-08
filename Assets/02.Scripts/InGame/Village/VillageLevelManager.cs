using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class VillageLevelManager : MonoBehaviourPunCallbacks
{
    public static VillageLevelManager Instance { get; private set; }

    [SerializeField] private VillageLevelRequirementSO _requirementSO;

    public int CurrentLevel { get; private set; } = 1;
    public int CurrentGauge { get; private set; }
    public int CurrentThreshold => GetThresholdForLevel(CurrentLevel);
    private bool CanLevelUp => !_requirementSO.IsMaxLevel(CurrentLevel)
                               && CurrentGauge >= CurrentThreshold
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
        _subscribed = true;
    }

    private void HandleBuildingBuilt(BuildingDataSO data)
    {
        if (data == null) return;

        bool isFirst = _builtOnceIds.Add(data.BuildingId);
        if (isFirst && data.FirstBuildGaugeContribution > 0)
        {
            CurrentGauge += data.FirstBuildGaugeContribution;
        }

        if (CanLevelUp)
        {
            RequestLevelUp();
        }
        OnVillageStateChanged?.Invoke();
    }

    public bool IsBuildingUnlocked(BuildingDataSO data) => data != null && CurrentLevel >= data.RequiredVillageLevel;

    private void RequestLevelUp()
    {
        if (!CanLevelUp) return;

        if (!PhotonNetwork.IsConnected)
        {
            CurrentGauge -= CurrentThreshold;
            CurrentLevel++;
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

        int threshold = CurrentThreshold;
        CurrentGauge -= threshold;
        CurrentLevel++;

        photonView.RpcSafe(nameof(RPC_SetVillageLevel), RpcTarget.Others, CurrentLevel, CurrentGauge);
        OnVillageStateChanged?.Invoke();
    }

    [PunRPC]
    private void RPC_SetVillageLevel(int level, int gauge)
    {
        CurrentLevel = level;
        CurrentGauge = gauge;
        OnVillageStateChanged?.Invoke();
    }

    private int GetThresholdForLevel(int level)
    {
        return _requirementSO.GetRequirement(level).GaugeThreshold;
    }

    private int GetBuildingCountForLevel(int level)
    {
        return _requirementSO.GetRequirement(level).BuildingCountThreshold;
    }

    #region Sync
    
    public void ImportSaveData(VillageSaveData saveData)
    {
        CurrentLevel = Math.Max(saveData.Level, 1);
        CurrentGauge = saveData.Gauge;
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
            Gauge = CurrentGauge,
            BuiltBuildings = _builtOnceIds.ToList()
        };
    }
    #endregion

}