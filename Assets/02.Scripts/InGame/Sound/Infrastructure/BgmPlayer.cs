using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// AudioSource 기반 BGM 재생기.
/// 크로스페이드, 페이드 인/아웃을 UniTask로 처리한다.
/// </summary>
public class BgmPlayer : MonoBehaviour, IBgmPlayer
{
    private const float DefaultBgmBaseVolume = 0.2f;
    private AudioSource _source;
    private AudioSource _overlaySource;
    private AudioMixerGroup _musicGroup;
    private float _baseVolume = DefaultBgmBaseVolume;
    private CancellationTokenSource _fadeCts;
    private CancellationTokenSource _mainVolumeCts;
    private CancellationTokenSource _overlayFadeCts;
    private string _currentClipKey;
    private string _currentOverlayClipKey;
    private float _mainVolumeBeforeDuck = DefaultBgmBaseVolume;
    private bool _isMainDucked;

    // 초기화: BGM 전용 AudioSource를 생성한다
    public void Initialize(Transform parent, AudioMixerGroup musicGroup)
    {
        _musicGroup = musicGroup;

        GameObject bgmObj = new GameObject("BGM_Source");
        bgmObj.transform.SetParent(parent);
        _source = bgmObj.AddComponent<AudioSource>();
        _source.outputAudioMixerGroup = _musicGroup;
        _source.loop = true;
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;

        GameObject overlayObj = new GameObject("BGM_Overlay_Source");
        overlayObj.transform.SetParent(parent);
        _overlaySource = overlayObj.AddComponent<AudioSource>();
        _overlaySource.outputAudioMixerGroup = _musicGroup;
        _overlaySource.loop = true;
        _overlaySource.playOnAwake = false;
        _overlaySource.spatialBlend = 0f;
    }

    // 단일 클립으로 크로스페이드 전환한다
    public async UniTask CrossfadeAsync(string clipKey, BgmTransitionConfig config, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(clipKey))
            return;

        if (_source == null)
        {
            Debug.LogWarning("[BgmPlayer] AudioSource가 아직 초기화되지 않아 BGM 재생을 건너뜁니다.");
            return;
        }

        if (_source.isPlaying && _currentClipKey == clipKey)
            return;

        // 진행 중인 페이드 취소
        CancelCurrentFade();
        CancelMainVolumeFade();
        _isMainDucked = false;
        CancellationToken linkedCt = CreateLinkedToken(ct);

        try
        {
            while (ResourceManager.Instance == null)
            {
                linkedCt.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, linkedCt);
            }

            // 기존 BGM 페이드 아웃
            if (_source.isPlaying && config.FadeOutDuration > 0f)
            {
                await FadeVolumeAsync(_source, _source.volume, 0f, config.FadeOutDuration, linkedCt);
            }
            _source.Stop();
            _currentClipKey = null;

            // 새 클립 로드
            AudioClip clip = await ResourceManager.Instance.LoadAsync<AudioClip>(clipKey);
            if (clip == null)
            {
                Debug.LogError($"[BgmPlayer] BGM 클립 로드 실패: {clipKey}");
                return;
            }

            // 새 BGM 페이드 인
            _source.clip = clip;
            _source.loop = true;
            _source.volume = 0f;
            _source.Play();
            _currentClipKey = clipKey;

            float targetVolume = _baseVolume * config.VolumeMultiplier;
            await FadeVolumeAsync(_source, 0f, targetVolume, config.FadeInDuration, linkedCt);
        }
        catch (System.OperationCanceledException)
        {
        }
    }

    public async UniTask DuckMainAsync(float fadeDuration, CancellationToken ct = default)
    {
        if (_source == null)
            return;

        CancelMainVolumeFade();
        CancellationToken linkedCt = CreateMainVolumeLinkedToken(ct);

        try
        {
            if (!_isMainDucked)
            {
                _mainVolumeBeforeDuck = _source.volume;
                _isMainDucked = true;
            }

            await FadeVolumeAsync(_source, _source.volume, 0f, fadeDuration, linkedCt);
        }
        catch (System.OperationCanceledException)
        {
        }
    }

    public async UniTask RestoreMainAsync(float fadeDuration, CancellationToken ct = default)
    {
        if (_source == null || !_isMainDucked)
            return;

        CancelMainVolumeFade();
        CancellationToken linkedCt = CreateMainVolumeLinkedToken(ct);

        try
        {
            await FadeVolumeAsync(_source, _source.volume, _mainVolumeBeforeDuck, fadeDuration, linkedCt);
            _isMainDucked = false;
        }
        catch (System.OperationCanceledException)
        {
        }
    }

    public async UniTask PlayOverlayAsync(string clipKey, BgmTransitionConfig config, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(clipKey) || _overlaySource == null)
            return;

        if (_overlaySource.isPlaying && _currentOverlayClipKey == clipKey)
            return;

        CancelOverlayFade();
        CancellationToken linkedCt = CreateOverlayLinkedToken(ct);

        try
        {
            while (ResourceManager.Instance == null)
            {
                linkedCt.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, linkedCt);
            }

            if (_overlaySource.isPlaying && config.FadeOutDuration > 0f)
                await FadeVolumeAsync(_overlaySource, _overlaySource.volume, 0f, config.FadeOutDuration, linkedCt);

            _overlaySource.Stop();
            _currentOverlayClipKey = null;

            AudioClip clip = await ResourceManager.Instance.LoadAsync<AudioClip>(clipKey);
            if (clip == null)
            {
                Debug.LogError($"[BgmPlayer] Overlay BGM 클립 로드 실패: {clipKey}");
                return;
            }

            _overlaySource.clip = clip;
            _overlaySource.loop = true;
            _overlaySource.volume = 0f;
            _overlaySource.Play();
            _currentOverlayClipKey = clipKey;

            float targetVolume = _baseVolume * config.VolumeMultiplier;
            await FadeVolumeAsync(_overlaySource, 0f, targetVolume, config.FadeInDuration, linkedCt);
        }
        catch (System.OperationCanceledException)
        {
        }
    }

    public async UniTask StopOverlayAsync(float fadeDuration, CancellationToken ct = default)
    {
        if (_overlaySource == null)
            return;

        CancelOverlayFade();
        CancellationToken linkedCt = CreateOverlayLinkedToken(ct);

        try
        {
            if (_overlaySource.isPlaying)
                await FadeVolumeAsync(_overlaySource, _overlaySource.volume, 0f, fadeDuration, linkedCt);

            _overlaySource.Stop();
            _overlaySource.clip = null;
            _overlaySource.loop = true;
            _currentOverlayClipKey = null;
        }
        catch (System.OperationCanceledException)
        {
        }
    }

    // [확장] 인트로/루프 분리 재생이 필요하면 CrossfadeWithIntroAsync 구현 추가
    // 인트로 클립 1회 재생 → 루프 클립 무한 반복 패턴

    // 페이드 아웃 후 정지한다
    public async UniTask StopAsync(float fadeDuration, CancellationToken ct = default)
    {
        if (_source == null)
            return;

        CancelCurrentFade();
        CancelMainVolumeFade();
        CancellationToken linkedCt = CreateLinkedToken(ct);

        await FadeVolumeAsync(_source, _source.volume, 0f, fadeDuration, linkedCt);
        _source.Stop();
        _source.clip = null;
        _source.loop = true;
        _currentClipKey = null;
        _isMainDucked = false;
    }

    // 즉시 정지한다
    public void Stop()
    {
        if (_source == null)
            return;

        CancelCurrentFade();
        CancelMainVolumeFade();
        _source.Stop();
        _source.clip = null;
        _source.loop = true;
        _baseVolume = DefaultBgmBaseVolume;
        _currentClipKey = null;
        _isMainDucked = false;
    }

    // 일시 정지한다
    public void Pause()
    {
        _source.Pause();
    }

    // 일시 정지를 해제한다
    public void Resume()
    {
        _source.UnPause();
    }

    private async UniTask FadeVolumeAsync(AudioSource source, float from, float to, float duration, CancellationToken ct)
    {
        if (source == null)
            return;

        if (duration <= 0f)
        {
            source.volume = to;
            return;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            ct.ThrowIfCancellationRequested();
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(from, to, elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        source.volume = to;
    }

    private void CancelCurrentFade()
    {
        if (_fadeCts != null)
        {
            _fadeCts.Cancel();
            _fadeCts.Dispose();
        }
        _fadeCts = new CancellationTokenSource();
    }

    private void CancelMainVolumeFade()
    {
        if (_mainVolumeCts != null)
        {
            _mainVolumeCts.Cancel();
            _mainVolumeCts.Dispose();
        }
        _mainVolumeCts = new CancellationTokenSource();
    }

    private void CancelOverlayFade()
    {
        if (_overlayFadeCts != null)
        {
            _overlayFadeCts.Cancel();
            _overlayFadeCts.Dispose();
        }
        _overlayFadeCts = new CancellationTokenSource();
    }

    private CancellationToken CreateLinkedToken(CancellationToken external)
    {
        if (external == default) return _fadeCts.Token;
        return CancellationTokenSource.CreateLinkedTokenSource(_fadeCts.Token, external).Token;
    }

    private CancellationToken CreateMainVolumeLinkedToken(CancellationToken external)
    {
        if (external == default) return _mainVolumeCts.Token;
        return CancellationTokenSource.CreateLinkedTokenSource(_mainVolumeCts.Token, external).Token;
    }

    private CancellationToken CreateOverlayLinkedToken(CancellationToken external)
    {
        if (external == default) return _overlayFadeCts.Token;
        return CancellationTokenSource.CreateLinkedTokenSource(_overlayFadeCts.Token, external).Token;
    }

    private void OnDestroy()
    {
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _mainVolumeCts?.Cancel();
        _mainVolumeCts?.Dispose();
        _overlayFadeCts?.Cancel();
        _overlayFadeCts?.Dispose();
    }
}
