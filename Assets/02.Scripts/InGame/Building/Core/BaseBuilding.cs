using Photon.Pun;
using UnityEngine;

public abstract class BaseBuilding : MonoBehaviour
{
    public BuildingDataSO BuildingData { get; private set; }
    public BuildingSaveData SaveData { get; private set; }
    
    public float ConstructionProgress { get; private set; }
    public bool IsConstructionComplete => SaveData == null || SaveData.RemainingDays <= 0;

    public void Initialize(BuildingDataSO buildingData, BuildingSaveData saveData)
    {
        BuildingData = buildingData;
        SetConstructionState(saveData);
        OnBuildingInitialized();
    }

    public void SetConstructionState(BuildingSaveData saveData)
    {
        SaveData = CloneSaveData(saveData);
        ConstructionProgress = CalculateConstructionProgress();
        OnConstructionStateChanged(ConstructionProgress, IsConstructionComplete);
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

    private float CalculateConstructionProgress()
    {
        if (BuildingData == null || BuildingData.ConstructionDays <= 0) return 1f;
        if (SaveData == null) return 1f;
        return Mathf.Clamp01(1f - (float)SaveData.RemainingDays / BuildingData.ConstructionDays);
    }

    private static BuildingSaveData CloneSaveData(BuildingSaveData source)
    {
        if (source == null) return null;

        return new BuildingSaveData
        {
            BuildingId = source.BuildingId,
            AnchorX = source.AnchorX,
            AnchorY = source.AnchorY,
            AnchorZ = source.AnchorZ,
            Direction = source.Direction,
            Swapped = source.Swapped,
            RemainingDays = source.RemainingDays
        };
    }
}