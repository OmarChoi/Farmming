using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

/// <summary>
/// ObjectPool 기반 SFX 재생기.
/// 2D/3D/Transform 추적 모드를 지원하며, 재생 완료 후 자동으로 풀에 반환한다.
/// </summary>
public class SfxPlayer : MonoBehaviour, ISfxPlayer
{
    private ObjectPool<AudioSource> _pool;
    private Transform _poolRoot;
    private AudioMixerGroup _effectGroup;
    private readonly Dictionary<string, LoopingSfxState> _loopingSources = new Dictionary<string, LoopingSfxState>();

    private const int DefaultPoolSize = 15;
    private const int MaxPoolSize = 30;

    private sealed class LoopingSfxState
    {
        public AudioSource Source;
        public bool StopRequested;
        public bool IsStopping;
        public readonly float TargetVolume;

        public LoopingSfxState(float targetVolume)
        {
            TargetVolume = targetVolume;
        }
    }

    // 초기화: SFX Pool을 생성한다
    public void Initialize(Transform parent, AudioMixerGroup effectGroup, int poolSize = DefaultPoolSize)
    {
        _poolRoot = parent;
        _effectGroup = effectGroup;

        _pool = new ObjectPool<AudioSource>(
            createFunc: CreateSource,
            actionOnGet: OnGetSource,
            actionOnRelease: OnReleaseSource,
            actionOnDestroy: OnDestroySource,
            collectionCheck: false,
            defaultCapacity: poolSize,
            maxSize: MaxPoolSize
        );
    }

    // SfxPlayRequest에 따라 효과음을 재생한다
    public void Play(SfxPlayRequest request)
    {
        if (string.IsNullOrEmpty(request.ClipKey)) return;
        PlayInternalAsync(request).Forget();
    }

    public void PlayForDuration(SfxPlayRequest request, float duration, float fadeOutDuration)
    {
        if (string.IsNullOrEmpty(request.ClipKey)) return;
        PlayForDurationInternalAsync(request, duration, fadeOutDuration).Forget();
    }

    public void PlayLooping(string loopKey, SfxPlayRequest request, float fadeInDuration)
    {
        if (string.IsNullOrEmpty(request.ClipKey)) return;

        string safeLoopKey = string.IsNullOrEmpty(loopKey) ? request.ClipKey : loopKey;
        if (_loopingSources.ContainsKey(safeLoopKey))
            return;

        LoopingSfxState state = new LoopingSfxState(Mathf.Max(0f, request.Volume));
        _loopingSources.Add(safeLoopKey, state);
        PlayLoopingInternalAsync(safeLoopKey, request, Mathf.Max(0f, fadeInDuration), state).Forget();
    }

    public void StopLooping(string loopKey, float fadeOutDuration)
    {
        if (string.IsNullOrEmpty(loopKey)) return;
        if (!_loopingSources.TryGetValue(loopKey, out LoopingSfxState state))
            return;

        state.StopRequested = true;

        if (state.Source == null)
        {
            _loopingSources.Remove(loopKey);
            return;
        }

        StopLoopingInternalAsync(loopKey, state, Mathf.Max(0f, fadeOutDuration)).Forget();
    }

    private async UniTaskVoid PlayInternalAsync(SfxPlayRequest request)
    {
        // 클립 로드
        AudioClip clip = await ResourceManager.Instance.LoadAsync<AudioClip>(request.ClipKey);
        if (clip == null)
        {
            Debug.LogWarning($"[SfxPlayer] SFX 클립 로드 실패: {request.ClipKey}");
            return;
        }

        // 풀에서 AudioSource 획득
        AudioSource source = _pool.Get();
        if (source == null) return;

        // 클립 설정 및 공간 모드 적용
        source.clip = clip;
        source.loop = false;
        source.volume = Mathf.Max(0f, request.Volume);
        source.pitch = Mathf.Max(0.01f, request.Pitch);
        ConfigureSpatial(source, request);
        source.Play();

        // 재생 완료 대기 후 풀 반환
        await WaitAndReturnAsync(source, request.ESpatialMode == ESpatialMode.FollowTransform ? request.FollowTarget : null);
    }

    private async UniTaskVoid PlayForDurationInternalAsync(SfxPlayRequest request, float duration, float fadeOutDuration)
    {
        AudioClip clip = await ResourceManager.Instance.LoadAsync<AudioClip>(request.ClipKey);
        if (clip == null)
        {
            Debug.LogWarning($"[SfxPlayer] SFX 클립 로드 실패: {request.ClipKey}");
            return;
        }

        AudioSource source = _pool.Get();
        if (source == null) return;

        float safeDuration = Mathf.Max(0.01f, duration);

        source.clip = clip;
        source.loop = clip.length < safeDuration;
        source.volume = Mathf.Max(0f, request.Volume);
        source.pitch = Mathf.Max(0.01f, request.Pitch);
        ConfigureSpatial(source, request);
        source.Play();

        await WaitTimedAndReturnAsync(
            source,
            request.ESpatialMode == ESpatialMode.FollowTransform ? request.FollowTarget : null,
            safeDuration,
            fadeOutDuration,
            Mathf.Max(0f, request.Volume));
    }

    private async UniTaskVoid PlayLoopingInternalAsync(string loopKey, SfxPlayRequest request, float fadeInDuration, LoopingSfxState state)
    {
        AudioClip clip = await ResourceManager.Instance.LoadAsync<AudioClip>(request.ClipKey);
        if (clip == null)
        {
            Debug.LogWarning($"[SfxPlayer] SFX ?대┰ 濡쒕뱶 ?ㅽ뙣: {request.ClipKey}");
            RemoveLoopingState(loopKey, state);
            return;
        }

        if (state.StopRequested)
        {
            RemoveLoopingState(loopKey, state);
            return;
        }

        AudioSource source = _pool.Get();
        if (source == null)
        {
            RemoveLoopingState(loopKey, state);
            return;
        }

        state.Source = source;
        Transform followTarget = request.ESpatialMode == ESpatialMode.FollowTransform ? request.FollowTarget : null;

        source.clip = clip;
        source.loop = true;
        source.volume = fadeInDuration > 0f ? 0f : state.TargetVolume;
        source.pitch = Mathf.Max(0.01f, request.Pitch);
        ConfigureSpatial(source, request);
        source.Play();

        await KeepLoopingSourceAliveAsync(source, followTarget, fadeInDuration, state);
    }

    private async UniTask KeepLoopingSourceAliveAsync(AudioSource source, Transform followTarget, float fadeInDuration, LoopingSfxState state)
    {
        bool isFollowing = followTarget != null;
        float elapsed = 0f;

        while (source != null && source.isPlaying && !state.StopRequested)
        {
            if (isFollowing && followTarget != null)
                source.transform.position = followTarget.position;

            if (fadeInDuration > 0f && elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(0f, state.TargetVolume, Mathf.Clamp01(elapsed / fadeInDuration));
            }
            else
            {
                source.volume = state.TargetVolume;
            }

            await UniTask.Yield();
        }
    }

    private async UniTaskVoid StopLoopingInternalAsync(string loopKey, LoopingSfxState state, float fadeOutDuration)
    {
        if (state.IsStopping)
            return;

        state.IsStopping = true;
        AudioSource source = state.Source;

        if (source != null)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (source != null && elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / fadeOutDuration));
                await UniTask.Yield();
            }

            if (source != null)
            {
                source.Stop();
                source.loop = false;
                _pool.Release(source);
            }
        }

        RemoveLoopingState(loopKey, state);
    }

    private void RemoveLoopingState(string loopKey, LoopingSfxState state)
    {
        if (_loopingSources.TryGetValue(loopKey, out LoopingSfxState current) && current == state)
            _loopingSources.Remove(loopKey);
    }

    private void ConfigureSpatial(AudioSource source, SfxPlayRequest request)
    {
        switch (request.ESpatialMode)
        {
            case ESpatialMode.Flat2D:
                source.spatialBlend = 0f;
                source.transform.SetParent(_poolRoot);
                source.transform.localPosition = Vector3.zero;
                break;

            case ESpatialMode.Positional3D:
                source.spatialBlend = 1f;
                source.transform.SetParent(_poolRoot);
                source.transform.position = request.Position;
                break;

            case ESpatialMode.FollowTransform:
                source.spatialBlend = 1f;
                source.transform.SetParent(_poolRoot);
                if (request.FollowTarget != null)
                    source.transform.position = request.FollowTarget.position;
                break;
        }
    }

    private async UniTask WaitAndReturnAsync(AudioSource source, Transform followTarget)
    {
        bool isFollowing = followTarget != null;

        while (source != null && source.isPlaying)
        {
            // FollowTarget 위치 동기화 (타겟 파괴 시 마지막 위치에서 잔여 재생)
            if (isFollowing && followTarget != null)
            {
                source.transform.position = followTarget.position;
            }
            await UniTask.Yield();
        }

        if (source == null) return;

        source.clip = null;
        source.transform.SetParent(_poolRoot);
        _pool.Release(source);
    }

    private async UniTask WaitTimedAndReturnAsync(AudioSource source, Transform followTarget, float duration, float fadeOutDuration, float startVolume)
    {
        bool isFollowing = followTarget != null;
        float safeFadeOutDuration = Mathf.Clamp(fadeOutDuration, 0f, duration);
        float fadeStartTime = duration - safeFadeOutDuration;
        float elapsed = 0f;

        while (source != null && source.isPlaying && elapsed < duration)
        {
            if (isFollowing && followTarget != null)
            {
                source.transform.position = followTarget.position;
            }

            if (safeFadeOutDuration > 0f && elapsed >= fadeStartTime)
            {
                float fadeProgress = Mathf.Clamp01((elapsed - fadeStartTime) / safeFadeOutDuration);
                source.volume = Mathf.Lerp(startVolume, 0f, fadeProgress);
            }

            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }

        if (source == null) return;

        source.Stop();
        source.loop = false;
        source.volume = 1f;
        source.pitch = 1f;
        source.clip = null;
        source.transform.SetParent(_poolRoot);
        _pool.Release(source);
    }

    private AudioSource CreateSource()
    {
        GameObject obj = new GameObject("SFX_Source");
        obj.transform.SetParent(_poolRoot);
        AudioSource source = obj.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = _effectGroup;
        source.playOnAwake = false;
        source.loop = false;
        return source;
    }

    private void OnGetSource(AudioSource source)
    {
        source.gameObject.SetActive(true);
        source.loop = false;
        source.volume = 1f;
        source.pitch = 1f;
    }

    private void OnReleaseSource(AudioSource source)
    {
        source.Stop();
        source.loop = false;
        source.volume = 1f;
        source.pitch = 1f;
        source.clip = null;
        source.gameObject.SetActive(false);
    }

    private void OnDestroySource(AudioSource source)
    {
        if (source != null) Destroy(source.gameObject);
    }

    private void OnDestroy()
    {
        foreach (LoopingSfxState state in _loopingSources.Values)
        {
            state.StopRequested = true;
            if (state.Source != null)
                state.Source.Stop();
        }
        _loopingSources.Clear();
        _pool?.Dispose();
    }
}
