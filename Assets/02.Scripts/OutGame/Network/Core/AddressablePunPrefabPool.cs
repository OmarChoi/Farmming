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
/// Addressables로 미리 로드한 네트워크 프리팹을 PUN의 동기 PrefabPool 인터페이스에 제공한다.
/// </summary>
/// <remarks>
/// ResourceManager보다 먼저 방 입장 게이트에서 동작할 수 있고 Addressables handle 수명을 직접 관리해야 하므로 이 풀은 Addressables를 직접 호출한다.
/// </remarks>
public sealed class AddressablePunPrefabPool : IPunPrefabPool
{
    private readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _handles = new Dictionary<string, AsyncOperationHandle<GameObject>>();

    public bool IsPreloaded { get; private set; }

    /// <summary>
    /// PhotonNetwork.Instantiate 호출 전에 필요한 네트워크 프리팹을 Addressables에서 모두 로드해 캐시한다.
    /// </summary>
    public async UniTask PreloadAsync(IEnumerable<string> prefabIds)
    {
        if (prefabIds == null) throw new ArgumentNullException(nameof(prefabIds));

        // 프리로드 시간과 중복 키를 추적해 Addressables 설정 문제를 초기에 드러낸다.
        var stopwatch = Stopwatch.StartNew();
        var seen = new HashSet<string>();
        int loadedCount = 0;

        foreach (string prefabId in prefabIds)
        {
            // 빈 키나 중복 키는 런타임 캐시 미스로 넘기지 않고 preload 단계에서 실패시킨다.
            if (string.IsNullOrEmpty(prefabId))
                throw new InvalidOperationException("[AddressablePunPrefabPool] Network prefab key is null or empty.");

            if (!seen.Add(prefabId))
                throw new InvalidOperationException($"[AddressablePunPrefabPool] Duplicate network prefab key: {prefabId}");

            if (_prefabs.ContainsKey(prefabId))
                continue;

            // Addressables 로드는 preload에서만 수행하고 Instantiate에서는 절대 동기 폴백을 하지 않는다.
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

            // 로드 결과와 PhotonView 구성을 검증해 방 진입 전에 네트워크 프리팹 불일치를 차단한다.
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

            // 프리팹과 Addressables handle을 함께 보관해 세션 종료 시점에 한 번에 해제할 수 있게 한다.
            _handles[prefabId] = handle;
            _prefabs[prefabId] = prefab;
            loadedCount++;
        }

        IsPreloaded = true;
        stopwatch.Stop();
        Debug.Log($"[AddressablePunPrefabPool] Preloaded {loadedCount} network prefabs in {stopwatch.ElapsedMilliseconds}ms.");
    }

    /// <summary>
    /// PUN이 요청한 prefabId의 비활성 인스턴스를 캐시에서 즉시 생성해 반환한다.
    /// </summary>
    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        // 캐시 미스는 preload 누락이므로 런타임 로드로 숨기지 않고 즉시 실패시킨다.
        if (!_prefabs.TryGetValue(prefabId, out GameObject prefab) || prefab == null)
        {
            Debug.LogError($"[AddressablePunPrefabPool] Cache miss for '{prefabId}'. EnsurePreloadedAsync must complete before PhotonNetwork.Instantiate.");
            return null;
        }

        // 원본 프리팹을 잠시 비활성화해 Awake/OnEnable이 PhotonView 할당 이전에 실행되는 일을 막는다.
        bool wasActive = prefab.activeSelf;
        if (wasActive)
            prefab.SetActive(false);

        // PUN의 PrefabPool 호출은 Unity 메인 스레드에서 이뤄진다는 전제로 DefaultPool 동작을 따른다.
        // PhotonView ID 할당과 활성화 이후에 Awake/OnEnable 흐름이 이어지게 하기 위한 처리다.
        GameObject instance = Object.Instantiate(prefab, position, rotation);

        if (wasActive)
            prefab.SetActive(true);

        if (instance.activeSelf)
            instance.SetActive(false);

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
    /// 세션 종료 등 안전한 시점에 캐시된 Addressables handle을 모두 해제한다.
    /// </summary>
    public void ReleaseAll()
    {
        // 살아있는 네트워크 인스턴스가 없다는 상위 정책이 보장된 뒤에만 호출한다.
        foreach (AsyncOperationHandle<GameObject> handle in _handles.Values)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }

        _handles.Clear();
        _prefabs.Clear();
        IsPreloaded = false;
    }
}
