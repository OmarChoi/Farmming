using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 건물 프리팹을 비동기로 로드해 인스턴스화한다.
/// 상태(Registry) 등록과 Initialize 호출은 호출자의 책임이다 — Factory는 생성/파기만 담당.
/// </summary>
public class BuildingInstanceFactory
{
    private readonly Transform _parent;

    public BuildingInstanceFactory(Transform parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// 프리팹을 로드해 월드에 스폰하고 BaseBuilding 컴포넌트를 반환한다.
    /// Initialize는 호출자가 직접 수행해야 한다 (구독 등록 순서 제어 목적).
    /// 프리팹이 없거나 컴포넌트가 없으면 null을 반환한다.
    /// </summary>
    public async UniTask<BaseBuilding> CreateAsync(
        BuildingDataSO data,
        Vector3 spawnPos,
        Quaternion rotation)
    {
        string prefab = AssetKey.NetworkPrefab.GetKey(data.BuildingId);
        if (string.IsNullOrEmpty(prefab)) return null;

        
        // TODO : 마스터클라이언트에서만 Instantiate하게 변경
        GameObject instance = PhotonNetwork.Instantiate(prefab, spawnPos, rotation);
        BaseBuilding building = instance.GetComponent<BaseBuilding>();
        if (building == null)
        {
            Object.Destroy(instance);
            return null;
        }

        return building;
    }

    /// <summary>
    /// 인스턴스를 씬에서 제거한다. 레지스트리에서의 해제는 호출자가 수행해야 한다.
    /// </summary>
    public void Destroy(BaseBuilding building)
    {
        if (building == null) return;
        Object.Destroy(building.gameObject);
    }
}
