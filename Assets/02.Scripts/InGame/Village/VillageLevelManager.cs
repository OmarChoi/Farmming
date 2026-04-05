using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class VillageLevelManager : MonoBehaviourPunCallbacks
{
    public static VillageLevelManager Instance { get; private set; }

    [SerializeField] private int[] _levelThresholds = { 100 };

    public int CurrentLevel { get; private set; } = 1;
    public int CurrentGauge { get; private set; }
    public int CurrentThreshold => GetThresholdForLevel(CurrentLevel);
    public bool CanLevelUp => CurrentThreshold > 0 && CurrentGauge >= CurrentThreshold;

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

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
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
        if (!_builtOnceIds.Add(data.BuildingId)) return;
        if (data.FirstBuildGaugeContribution <= 0)
        {
            OnVillageStateChanged?.Invoke();
            return;
        }
        CurrentGauge += data.FirstBuildGaugeContribution;
        OnVillageStateChanged?.Invoke();
        Debug.Log($"Gauge : {CurrentGauge}");
    }

    public bool IsBuildingUnlocked(BuildingDataSO data) => data != null && CurrentLevel >= data.RequiredVillageLevel;

    public void RequestLevelUp()
    {
        if (!CanLevelUp) return;

        if (!PhotonNetwork.IsConnected)
        {
            ExecuteLevelUp();
            return;
        }

        photonView.RpcSafe(nameof(RPC_RequestLevelUp), RpcTarget.MasterClient);
    }

    [PunRPC]
    private void RPC_RequestLevelUp()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!CanLevelUp) return;

        photonView.RpcSafe(nameof(RPC_ExecuteLevelUp), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_ExecuteLevelUp() => ExecuteLevelUp();

    private void ExecuteLevelUp()
    {
        int threshold = CurrentThreshold;
        if (threshold <= 0 || CurrentGauge < threshold) return;

        CurrentGauge -= threshold;
        CurrentLevel++;
        OnVillageStateChanged?.Invoke();
    }

    private int GetThresholdForLevel(int level)
    {
        int idx = level - 1;
        if (idx < 0 || idx >= _levelThresholds.Length) return 0;
        return _levelThresholds[idx];
    }
}
