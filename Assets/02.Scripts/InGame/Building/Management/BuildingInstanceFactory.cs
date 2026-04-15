using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 건물 프리팹을 비동기로 로드해 인스턴스화한다.
/// 상태(Registry) 등록과 Initialize 호출은 호출자의 책임이다 — Factory는 생성/파기만 담당.
/// 멀티 환경에서는 마스터만 PhotonNetwork.Instantiate/Destroy 호출. 클라이언트는 자동 복제로 받기만 한다.
/// </summary>
public class BuildingInstanceFactory
{
    /// <summary>
    /// 프리팹을 로드해 월드에 스폰하고 BaseBuilding 컴포넌트를 반환한다.
    /// Initialize는 호출자가 직접 수행해야 한다 (구독 등록 순서 제어 목적).
    /// 클라이언트는 항상 null을 반환한다 — 마스터의 자동 복제로만 인스턴스를 받는다.
    /// </summary>
    public async UniTask<BaseBuilding> CreateAsync(
        BuildingDataSO data,
        Vector3 spawnPos,
        Quaternion rotation,
        BuildingSaveData saveData = null)
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            return null;

        string prefab = AssetKey.NetworkPrefab.GetKey(data.BuildingId);
        if (string.IsNullOrEmpty(prefab)) return null;

        object[] instData = saveData != null
            ? new object[]
            {
                saveData.BuildingId,
                saveData.AnchorX, saveData.AnchorY, saveData.AnchorZ,
                saveData.Direction,
                saveData.RemainingDays,
            }
            : null;

        GameObject instance = PhotonNetwork.Instantiate(prefab, spawnPos, rotation, 0, instData);
        BaseBuilding building = instance.GetComponent<BaseBuilding>();
        if (building == null)
        {
            Object.Destroy(instance);
            return null;
        }

        return building;
    }

    /// <summary>
    /// 인스턴스를 씬에서 제거한다. 멀티 환경에서는 PhotonNetwork.Destroy로 자동 복제 파괴.
    /// 마스터(또는 owner)만 호출 가능. 레지스트리 해제는 호출자가 수행한다.
    /// </summary>
    public void Destroy(BaseBuilding building)
    {
        if (building == null) return;

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Destroy(building.gameObject);
        else
            Object.Destroy(building.gameObject);
    }
}