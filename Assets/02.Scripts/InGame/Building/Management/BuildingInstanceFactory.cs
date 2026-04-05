using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 건물 프리팹을 비동기로 로드해 인스턴스화하고, 초기화까지 수행한다.
/// 상태(Registry) 등록은 호출자의 책임이다 — Factory는 생성/파기만 담당.
/// </summary>
public class BuildingInstanceFactory
{
    private readonly Transform _parent;

    public BuildingInstanceFactory(Transform parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// 프리팹을 로드해 월드에 스폰하고 <see cref="BaseBuilding"/>을 초기화한다.
    /// 프리팹이 없거나 컴포넌트가 없으면 null을 반환한다.
    /// </summary>
    public async UniTask<BaseBuilding> CreateAsync(
        BuildingDataSO data,
        BuildingSaveData saveData,
        Vector3 spawnPos,
        Quaternion rotation,
        BuildingConstructionContext context)
    {
        string prefabKey = AssetKey.Building.GetKey(data.BuildingId);
        if (string.IsNullOrEmpty(prefabKey)) return null;

        GameObject prefab = await ResourceManager.Instance.LoadAsync<GameObject>(prefabKey);
        if (prefab == null) return null;

        GameObject instance = Object.Instantiate(prefab, spawnPos, rotation, _parent);
        BaseBuilding building = instance.GetComponent<BaseBuilding>();
        if (building == null)
        {
            Object.Destroy(instance);
            return null;
        }

        building.Initialize(data, saveData, context);
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
