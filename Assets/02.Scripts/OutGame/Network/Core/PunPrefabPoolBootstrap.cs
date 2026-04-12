using System;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Addressables 기반 PUN PrefabPool을 설치하고 네트워크 프리팹 preload를 단일 작업으로 조율한다.
/// </summary>
public static class PunPrefabPoolBootstrap
{
    private static readonly object _gate = new object();

    private static AddressablePunPrefabPool _pool;
    private static UniTaskCompletionSource _preloadCompletion;
    private static bool _preloadInProgress;
    private static bool _releaseRegistered;

    public static AddressablePunPrefabPool Pool => _pool;

    /// <summary>
    /// PUN PrefabPool이 설치되고 모든 네트워크 프리팹이 로드될 때까지 기다린다.
    /// </summary>
    public static async UniTask EnsurePreloadedAsync()
    {
        // PhotonNetwork.PrefabPool 설정과 Addressables 접근은 Unity 메인 스레드에서 처리한다.
        await UniTask.SwitchToMainThread();

        UniTask task;
        lock (_gate)
        {
            // 동시에 여러 진입점이 호출되더라도 같은 UniTaskCompletionSource를 공유한다.
            EnsurePoolAssigned();

            if (_pool.IsPreloaded)
                return;

            if (_preloadCompletion == null || !_preloadInProgress)
            {
                _preloadCompletion = new UniTaskCompletionSource();
                _preloadInProgress = true;
                PreloadInternalAsync(_preloadCompletion).Forget();
            }

            task = _preloadCompletion.Task;
        }

        await task;
    }

    /// <summary>
    /// 네트워크 프리팹 preload를 시도하고 실패 콜백을 통해 호출부별 복구 처리를 위임한다.
    /// </summary>
    public static async UniTask<bool> TryEnsurePreloadedAsync(Action<Exception> onFailed = null)
    {
        try
        {
            // 실패는 예외로 유지하되 반복 호출부의 try-catch 중복은 이 헬퍼에서 흡수한다.
            await EnsurePreloadedAsync();
            return true;
        }
        catch (Exception e)
        {
            onFailed?.Invoke(e);
            return false;
        }
    }

    /// <summary>
    /// 안전한 종료 시점에 Addressables 프리팹 캐시와 진행 중인 preload 상태를 초기화한다.
    /// </summary>
    public static void ReleaseAll()
    {
        // 방 이탈이나 일반 씬 전환에서는 호출하지 않고 앱/세션 종료 정책에서만 사용한다.
        lock (_gate)
        {
            _pool?.ReleaseAll();
            _preloadCompletion = null;
            _preloadInProgress = false;
        }
    }

    /// <summary>
    /// 현재 PUN PrefabPool을 Addressables 기반 구현으로 보장한다.
    /// </summary>
    private static void EnsurePoolAssigned()
    {
        // 이미 동일한 풀로 교체된 경우 기존 인스턴스를 계속 사용한다.
        if (PhotonNetwork.PrefabPool is AddressablePunPrefabPool addressablePool)
        {
            _pool = addressablePool;
            RegisterReleaseOnQuit();
            return;
        }

        if (_pool == null)
        {
            _pool = new AddressablePunPrefabPool();
        }

        PhotonNetwork.PrefabPool = _pool;
        RegisterReleaseOnQuit();
    }

    /// <summary>
    /// 앱 종료 시 Addressables handle을 정리하도록 release 콜백을 한 번만 등록한다.
    /// </summary>
    private static void RegisterReleaseOnQuit()
    {
        if (!_releaseRegistered)
        {
            // v1 정책상 방 이탈/씬 전환에서는 유지하고 애플리케이션 종료 때만 handle을 정리한다.
            Application.quitting += ReleaseAll;
            _releaseRegistered = true;
        }
    }

    /// <summary>
    /// 네트워크 프리팹 preload를 실행하고 실패를 상위 게이트로 전파한다.
    /// </summary>
    private static async UniTask PreloadInternalAsync(UniTaskCompletionSource completion)
    {
        try
        {
            // 하나라도 실패하면 방 입장/씬 진입을 막기 위해 completion에 예외를 전파한다.
            await _pool.PreloadAsync(AssetKey.NetworkPrefab.All);
            completion.TrySetResult();
        }
        catch (Exception e)
        {
            Debug.LogError($"[PunPrefabPoolBootstrap] Network prefab preload failed. Blocking Photon room/scene entry.\n{e}");
            completion.TrySetException(e);
        }
        finally
        {
            // 진행 상태만 초기화해 실패 후 다음 호출에서 새 preload를 재시도할 수 있게 한다.
            lock (_gate)
            {
                if (_preloadCompletion == completion)
                    _preloadInProgress = false;
            }
        }
    }
}
