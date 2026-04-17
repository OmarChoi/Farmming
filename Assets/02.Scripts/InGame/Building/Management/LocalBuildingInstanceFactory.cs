using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 오프라인/싱글 모드에서 Addressables로 건물 프리팹을 로드해 Object.Instantiate로 생성한다.
/// Photon과 무관하므로 네트워크 연결 상태와 관계없이 동작하며,
/// BuildingManager는 PhotonNetwork.IsConnected == false 일 때 이 factory를 선택한다.
/// </summary>
public class LocalBuildingInstanceFactory : IBuildingInstanceFactory
{
    public async UniTask<BaseBuilding> CreateAsync(
        BuildingInfo info,
        Vector3 position,
        Quaternion rotation)
    {
        if (!info.IsValid) return null;

        string key = AssetKey.NetworkPrefab.GetKey(info.SaveData.BuildingId);
        if (string.IsNullOrEmpty(key)) return null;

        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);
        GameObject prefab = await handle.ToUniTask();
        if (prefab == null) return null;

        GameObject instance = Object.Instantiate(prefab, position, rotation);
        BaseBuilding building = instance != null ? instance.GetComponent<BaseBuilding>() : null;
        if (instance != null && building == null)
        {
            Object.Destroy(instance);
            return null;
        }

        return building;
    }

    public bool Destroy(BaseBuilding building)
    {
        if (building == null) return false;
        Object.Destroy(building.gameObject);
        return true;
    }
}
