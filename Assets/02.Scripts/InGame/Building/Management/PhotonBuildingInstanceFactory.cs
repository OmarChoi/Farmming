using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 네트워크 마스터에서 PhotonNetwork.Instantiate로 건물을 생성하고,
/// InstantiationData에 BuildingSaveData의 bootstrap 값을 실어 클라이언트로 전달한다.
/// 최종 RemainingDays 권위 상태는 BuildingManager의 save/snapshot 경로에서 복원한다.
/// 네트워크 클라이언트에서는 호출되지 않는다 — 마스터 복제로만 인스턴스를 받는다.
/// </summary>
public class PhotonBuildingInstanceFactory : IBuildingInstanceFactory
{
    public UniTask<BaseBuilding> CreateAsync(
        BuildingInfo info,
        Vector3 position,
        Quaternion rotation)
    {
        if (!info.IsValid) return UniTask.FromResult<BaseBuilding>(null);
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            return UniTask.FromResult<BaseBuilding>(null);

        string prefab = AssetKey.NetworkPrefab.GetKey(info.SaveData.BuildingId);
        if (string.IsNullOrEmpty(prefab)) return UniTask.FromResult<BaseBuilding>(null);

        BuildingSaveData saveData = info.SaveData;
        object[] instData = new object[]
        {
            saveData.BuildingId,
            saveData.AnchorX, saveData.AnchorY, saveData.AnchorZ,
            saveData.Direction,
            saveData.RemainingDays,
        };

        GameObject instance = PhotonNetwork.Instantiate(prefab, position, rotation, 0, instData);
        BaseBuilding building = instance != null ? instance.GetComponent<BaseBuilding>() : null;
        if (instance != null && building == null)
        {
            Object.Destroy(instance);
        }

        return UniTask.FromResult(building);
    }

    public bool Destroy(BaseBuilding building)
    {
        if (building == null) return false;

        // BuildingManager가 연결 상태와 factory를 Awake 1회 매칭하므로
        // 여기서 disconnect에 도달하는 것은 desync 신호다. 은밀한 Object.Destroy 폴백은
        // registry/placement만 정리되는 반쪽 삭제를 유발하므로 false로 caller가 상태를 유지하게 한다.
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError($"[{nameof(PhotonBuildingInstanceFactory)}] Destroy called while disconnected; expected unreachable.");
            return false;
        }

        PhotonNetwork.Destroy(building.gameObject);
        return true;
    }
}
