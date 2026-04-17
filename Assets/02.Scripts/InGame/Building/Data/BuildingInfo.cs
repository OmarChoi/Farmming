using UnityEngine;

/// <summary>
/// 건물의 런타임 도메인 값 묶음. Data/SaveData/Anchor/Footprint는 항상 함께 움직여야 하며
/// 분리되면 registry/placement 불일치가 쉽게 발생한다. 부산물(instance, onLoading)은 포함하지 않는다.
/// Anchor.y는 baseY로 보정된 값이며 SaveData.AnchorY와 일치한다.
/// </summary>
public readonly struct BuildingInfo
{
    public readonly BuildingDataSO Data;
    public readonly BuildingSaveData SaveData;
    public readonly Vector3Int Anchor;
    public readonly BuildingFootprint Footprint;

    public BuildingInfo(
        BuildingDataSO data,
        BuildingSaveData saveData,
        Vector3Int anchor,
        BuildingFootprint footprint)
    {
        Data = data;
        SaveData = saveData;
        Anchor = anchor;
        Footprint = footprint;
    }

    public bool IsValid => Data != null && SaveData != null;
}
