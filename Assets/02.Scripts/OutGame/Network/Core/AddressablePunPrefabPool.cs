using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

/// <summary>
/// Addressables로 미리 로드한 네트워크 프리팹을 PUN의 동기 PrefabPool 인터페이스로 중계한다.
/// Addressables handle 수명을 직접 관리해야 하므로 ResourceManager를 거치지 않고 Addressables를 직접 호출한다.
/// </summary>
public sealed class AddressablePunPrefabPool : IPunPrefabPool
{
    private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _handles = new Dictionary<string, AsyncOperationHandle<GameObject>>();

    public bool IsPreloaded { get; private set; }

    /// <summary>
    /// 주어진 키 집합을 Addressables로 로드해 캐시한다. 빈/중복 키와 PhotonView 누락은 여기서 실패시킨다.
    /// </summary>
    public async UniTask PreloadAsync(IEnumerable<string> prefabIds)
    {
        if (prefabIds == null) throw new ArgumentNullException(nameof(prefabIds));

        var stopwatch = Stopwatch.StartNew();
        var seen = new HashSet<string>();
        int loadedCount = 0;

        foreach (string prefabId in prefabIds)
        {
            // 런타임 캐시 미스 대신 preload 단계에서 빈/중복 키를 먼저 실패시킨다.
            if (string.IsNullOrEmpty(prefabId)) throw new InvalidOperationException("[AddressablePunPrefabPool] Network prefab key is null or empty.");
            if (!seen.Add(prefabId)) throw new InvalidOperationException($"[AddressablePunPrefabPool] Duplicate network prefab key: {prefabId}");
            if (_prefabs.ContainsKey(prefabId)) continue;

            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(prefabId);
            GameObject prefab = null;

            try
            {
                prefab = await handle.ToUniTask();
            }
            catch (Exception)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);

                throw;
            }

            if (prefab == null)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);

                throw new InvalidOperationException($"[AddressablePunPrefabPool] Failed to load network prefab: {prefabId}");
            }

            if (prefab.GetComponentInChildren<PhotonView>(true) == null)
            {
                Addressables.Release(handle);
                throw new InvalidOperationException($"[AddressablePunPrefabPool] Network prefab has no PhotonView: {prefabId}");
            }

            _handles[prefabId] = handle;
            _prefabs[prefabId] = prefab;
            loadedCount++;
        }

        IsPreloaded = true;
        stopwatch.Stop();
        Debug.Log($"[AddressablePunPrefabPool] Preloaded {loadedCount} network prefabs in {stopwatch.ElapsedMilliseconds}ms.");
    }

    /// <summary>
    /// 캐시된 프리팹에서 비활성 인스턴스를 생성해 PUN에 반환한다. 캐시 미스는 즉시 예외.
    /// </summary>
    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        if (!_prefabs.TryGetValue(prefabId, out GameObject prefab) || prefab == null)
        {
            throw new NullReferenceException($"[AddressablePunPrefabPool] prefabId '{prefabId}' not preloaded");
        }

        // PhotonView ID 할당 전에 Awake/OnEnable이 돌지 않도록 원본을 잠시 비활성화 후 Instantiate.
        bool wasActive = prefab.activeSelf;
        if (wasActive) prefab.SetActive(false);

        GameObject instance = Object.Instantiate(prefab, position, rotation);

        if (wasActive) prefab.SetActive(true);
        if (instance.activeSelf) instance.SetActive(false);

        return instance;
    }

    /// <summary>
    /// PUN이 반환한 네트워크 인스턴스를 파괴한다.
    /// </summary>
    public void Destroy(GameObject gameObject)
    {
        if (gameObject == null) return;
        Object.Destroy(gameObject);
    }

    /// <summary>
    /// 살아있는 네트워크 인스턴스가 없다는 상위 정책이 보장된 뒤에만 호출해야 한다.
    /// </summary>
    public void ReleaseAll()
    {
        foreach (AsyncOperationHandle<GameObject> handle in _handles.Values)
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }

        _handles.Clear();
        _prefabs.Clear();
        IsPreloaded = false;
    }
}
