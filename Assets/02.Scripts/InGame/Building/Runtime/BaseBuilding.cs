using System;
using Photon.Pun;
using UnityEngine;

public abstract class BaseBuilding : MonoBehaviour
{
    public event Action<BaseBuilding> ConstructionCompleted;
    public event Action<NpcController> BuildingNpcAssigned;

    public BuildingDataSO BuildingData { get; private set; }
    public BuildingSaveData SaveData { get; private set; }
    public BuildingConstructionContext ConstructionContext { get; private set; }
    public NpcController BuildingNpc => _buildingNpc;

    public float ConstructionProgress { get; private set; }
    public bool IsConstructionComplete => SaveData == null || SaveData.RemainingDays <= 0;

    private bool _isDayBound;
    private NpcController _buildingNpc;
    private bool _constructionCompletedHandled;
    private bool _onLoading;

    /// 클라이언트는 마스터의 PhotonNetwork.Instantiate 자동 복제로 생성되므로
    /// TryBuild의 Initialize 경로를 거치지 않는다. PhotonView.InstantiationData에서
    /// BuildingSaveData를 복원해 BuildingManager에 자가 등록한다.
    /// 마스터/로컬 모드는 TryBuild에서 명시적으로 Initialize되므로 여기선 skip.
    protected virtual void Start()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient) return;
        if (BuildingManager.Instance == null) return;
        BuildingManager.Instance.RegisterClientSpawnedBuilding(this);
    }

    public void Initialize(
        BuildingDataSO buildingData,
        BuildingSaveData saveData,
        BuildingConstructionContext constructionContext,
        bool onLoading = false)
    {
        BuildingData = buildingData;
        SaveData = saveData;
        ConstructionContext = constructionContext;
        if (onLoading)
        {
            _onLoading = true;
            GameSceneInit.OnCompleteInitialize += OnLoadingFinished;
        }
        ConstructionProgress = CalculateConstructionProgress();
        if (!IsConstructionComplete)
        {
            // 가져오기/복원 과정에서 동일한 런타임 객체가 두 번 이상 초기화 가능성이 존재해
            // 건물이 아직 건설 중일 때만 완료 콜백을 다시 엽니다.
            _constructionCompletedHandled = false;
        }

        if (_isDayBound)
        {
            TimeEvents.OnNetDayStarted -= AdvanceDay;
        }
        TimeEvents.OnNetDayStarted += AdvanceDay;
        _isDayBound = true;

        OnBuildingInitialized();
        OnConstructionStateChanged(ConstructionProgress, IsConstructionComplete);

        if (!IsConstructionComplete) return;

        // 복원/실경과 공통 경로. _constructionCompletedHandled 가드로 이벤트 중복을 막고,
        // NPC 스폰은 HandleConstructionCompleted 내부의 _onLoading 체크로 억제된다.
        TryHandleConstructionCompleted();
    }

    /// 저장/스냅샷 복원 경로에서 RemainingDays만 교체하고 시각 상태를 맞춘다.
    /// 이벤트는 _constructionCompletedHandled 가드로 최초 1회만 발생한다.
    public void ApplyConstructionRemainingDays(int remainingDays)
    {
        if (SaveData == null) return;

        SaveData.RemainingDays = remainingDays;
        ConstructionProgress = CalculateConstructionProgress();
        OnConstructionStateChanged(ConstructionProgress, IsConstructionComplete);

        if (!IsConstructionComplete)
        {
            _constructionCompletedHandled = false;
            return;
        }

        TryHandleConstructionCompleted();
    }

    private void OnLoadingFinished()
    {
        _onLoading = false;
        GameSceneInit.OnCompleteInitialize -= OnLoadingFinished;
    }
    
    protected virtual void OnDestroy()
    {
        if (NpcSpawnManager.Instance != null)
        {
            NpcSpawnManager.Instance.Despawn(_buildingNpc);
        }
        GameSceneInit.OnCompleteInitialize -= OnLoadingFinished;

        // 클라이언트는 PhotonNetwork.Destroy 자동 복제로 파괴되므로 _registry/_placement도 같이 정리.
        // 마스터는 TryRemoveResolved 경로가 _registry를 먼저 비운 후 Destroy하므로 여기선 skip.
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient && BuildingManager.Instance != null)
            BuildingManager.Instance.UnregisterClientSpawnedBuilding(this);

        if (!_isDayBound) return;

        TimeEvents.OnNetDayStarted -= AdvanceDay;
        _isDayBound = false;

    }

    private void AdvanceDay()
    {
        if (SaveData == null || SaveData.RemainingDays <= 0) return;

        SaveData.RemainingDays--;
        ConstructionProgress = CalculateConstructionProgress();
        OnConstructionStateChanged(ConstructionProgress, IsConstructionComplete);
        LogBuildingSync($"AdvanceDay id={SaveData.BuildingId} anchor=({SaveData.AnchorX},{SaveData.AnchorY},{SaveData.AnchorZ}) remaining={SaveData.RemainingDays}");

        if (SaveData.RemainingDays <= 0)
        {
            TryHandleConstructionCompleted();
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogBuildingSync(string message)
    {
        Debug.Log($"[BuildingSync] {message}");
    }

    public void SetNpc(NpcController npc)
    {
        if (_buildingNpc == npc) return;

        _buildingNpc = npc;
        if (npc != null)
            BuildingNpcAssigned?.Invoke(npc);
    }
    
    public void HandleConstructionCompleted()
    {
        // Loading 중에는 NPC 생성을 하지 않게 설정
        // NPC 생성은 마스터에서만 실행 (멀티 시 마스터가 NPC를 관리)
        if (!_onLoading && (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient))
        {
            BuildingNpcSpawner spawner = GetComponent<BuildingNpcSpawner>();
            if (spawner != null && spawner.HasValidData)
            {
                InitializeBuildingNpcAnchors(spawner);

                NpcController npc = spawner.SpawnNpc(SaveData);
                if (npc != null)
                {
                    SetNpc(npc);
                }
            }
        }

        OnConstructionCompleted();
        ConstructionCompleted?.Invoke(this);
    }

    protected virtual void OnBuildingInitialized() { }

    protected abstract void OnConstructionStateChanged(float progress, bool isComplete);

    protected abstract void OnConstructionCompleted();

    private void TryHandleConstructionCompleted()
    {
        if (_constructionCompletedHandled) return;

        _constructionCompletedHandled = true;
        HandleConstructionCompleted();
    }

    private float CalculateConstructionProgress()
    {
        if (BuildingData == null || BuildingData.ConstructionDays <= 0) return 1f;
        if (SaveData == null) return 1f;
        return Mathf.Clamp01(1f - (float)SaveData.RemainingDays / BuildingData.ConstructionDays);
    }

    private void InitializeBuildingNpcAnchors(BuildingNpcSpawner spawner)
    {
        if (spawner == null || !spawner.HasValidData || SaveData == null) return;

        string runtimeNpcKey = NpcRuntimeKeyUtility.CreateBuildingNpcKey(spawner.NpcData, SaveData);
        if (string.IsNullOrEmpty(runtimeNpcKey)) return;

        NpcLocationAnchor[] anchors = GetComponentsInChildren<NpcLocationAnchor>(true);
        if (anchors == null || anchors.Length == 0) return;

        for (int i = 0; i < anchors.Length; i++)
        {
            NpcLocationAnchor anchor = anchors[i];
            if (anchor == null) continue;

            anchor.SetRegisterOnEnable(false);
            anchor.Initialize(
                spawner.NpcData,
                runtimeNpcKey,
                anchor.LocationType,
                anchor.LocationKey,
                anchor.Point,
                anchor.StylingHideRoot);
        }
    }

    public void InitializeNpcAnchorsIfNeeded()
    {
        BuildingNpcSpawner spawner = GetComponent<BuildingNpcSpawner>();
        if (spawner == null || !spawner.HasValidData) return;

        InitializeBuildingNpcAnchors(spawner);
    }
}
