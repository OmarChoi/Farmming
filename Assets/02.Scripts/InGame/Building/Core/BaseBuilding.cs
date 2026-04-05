using Photon.Pun;
using UnityEngine;

public abstract class BaseBuilding : MonoBehaviour
{
    public BuildingDataSO BuildingData { get; private set; }
    public BuildingSaveData SaveData { get; private set; }
    public BuildingConstructionContext ConstructionContext { get; private set; }

    public float ConstructionProgress { get; private set; }
    public bool IsConstructionComplete => SaveData == null || SaveData.RemainingDays <= 0;

    private bool _isDayBound;
    private bool _constructionCompletedHandled;

    public void Initialize(BuildingDataSO buildingData, BuildingSaveData saveData, BuildingConstructionContext constructionContext)
    {
        BuildingData = buildingData;
        SaveData = saveData;
        ConstructionContext = constructionContext;
        ConstructionProgress = CalculateConstructionProgress();
        if (!IsConstructionComplete)
        {
            // Import/restore can reinitialize the same runtime object more than once.
            // Only reopen the completion callback when the building is still under construction.
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

        if (IsConstructionComplete)
        {
            TryHandleConstructionCompleted();
        }
    }

    protected virtual void OnDestroy()
    {
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

        if (SaveData.RemainingDays <= 0)
        {
            TryHandleConstructionCompleted();
        }
    }

    public void HandleConstructionCompleted()
    {
        // NPC 생성은 마스터에서만 실행 (멀티 시 마스터가 NPC를 관리)
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
        {
            BuildingNpcSpawner spawner = GetComponent<BuildingNpcSpawner>();
            if (spawner != null && spawner.HasValidData)
            {
                spawner.SpawnNpc();
            }
        }

        OnConstructionCompleted();
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
}
