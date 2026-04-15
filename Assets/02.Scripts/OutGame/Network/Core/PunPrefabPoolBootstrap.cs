using System;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Addressables 기반 PUN PrefabPool 설치 + 네트워크 프리팹 preload 게이트.
/// 호출은 Unity 메인 스레드에서만 이뤄진다는 전제.
/// </summary>
public static class PunPrefabPoolBootstrap
{
    private static AddressablePunPrefabPool _pool;
    private static UniTaskCompletionSource _preloadCompletion;
    private static bool _preloadInProgress;
    private static bool _releaseRegistered;

    public static AddressablePunPrefabPool Pool => _pool;

    /// <summary>
    /// PUN PrefabPool을 설치하고 모든 네트워크 프리팹 preload가 끝날 때까지 기다린다.
    /// 동시 호출은 하나의 preload 작업을 공유한다.
    /// </summary>
    public static async UniTask EnsurePreloadedAsync()
    {
#if UNITY_EDITOR
        // PUN/Addressables는 메인 스레드 전용. 위반 시 스레드풀 컨티뉴에이션에서 터지므로 개발 중 즉시 드러낸다.
        Debug.Assert(PlayerLoopHelper.MainThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId,
            "[PunPrefabPoolBootstrap] EnsurePreloadedAsync must run on the Unity main thread.");
#endif

        EnsurePoolAssigned();

        if (_pool.IsPreloaded) return;

        if (_preloadCompletion == null || !_preloadInProgress)
        {
            _preloadCompletion = new UniTaskCompletionSource();
            _preloadInProgress = true;
            PreloadInternalAsync(_preloadCompletion).Forget();
        }

        await _preloadCompletion.Task;
    }

    /// <summary>
    /// EnsurePreloadedAsync 래퍼. 실패 시 예외 대신 false와 콜백으로 전달한다.
    /// </summary>
    public static async UniTask<bool> TryEnsurePreloadedAsync(Action<Exception> onFailed = null)
    {
        try
        {
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
    /// Addressables 캐시와 preload 상태를 초기화한다. 앱 종료 시점에만 호출한다.
    /// </summary>
    public static void ReleaseAll()
    {
        _pool?.ReleaseAll();
        _preloadCompletion = null;
        _preloadInProgress = false;
    }

    /// <summary>
    /// PhotonNetwork.PrefabPool이 AddressablePunPrefabPool 인스턴스인지 확인/보장한다.
    /// </summary>
    private static void EnsurePoolAssigned()
    {
        if (PhotonNetwork.PrefabPool is AddressablePunPrefabPool addressablePool)
        {
            _pool = addressablePool;
            RegisterReleaseOnQuit();
            return;
        }

        _pool ??= new AddressablePunPrefabPool();
        PhotonNetwork.PrefabPool = _pool;
        RegisterReleaseOnQuit();
    }

    /// <summary>
    /// 앱 종료 시 Addressables handle을 해제하도록 콜백을 1회 등록한다.
    /// </summary>
    private static void RegisterReleaseOnQuit()
    {
        if (_releaseRegistered) return;
        
        // v1 정책: 방 이탈/씬 전환에서는 유지하고 앱 종료에서만 handle 해제.
        Application.quitting += ReleaseAll;
        _releaseRegistered = true;
    }

    /// <summary>
    /// 실제 preload 실행부. 결과/예외를 공유 completion으로 전파한다.
    /// </summary>
    private static async UniTask PreloadInternalAsync(UniTaskCompletionSource completion)
    {
        try
        {
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
            // 재시도 가능하도록 진행 플래그만 내린다. completion은 교체되지 않으면 놔둔다.
            if (_preloadCompletion == completion) _preloadInProgress = false;
        }
    }
}
