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
    private AudioMixerGroup _musicGroup;
    private float _baseVolume = DefaultBgmBaseVolume;
    private CancellationTokenSource _fadeCts;

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
    }

    // 단일 클립으로 크로스페이드 전환한다
    public async UniTask CrossfadeAsync(string clipKey, BgmTransitionConfig config, CancellationToken ct = default)
    {
        if (_source == null)
        {
            Debug.LogWarning("[BgmPlayer] AudioSource가 아직 초기화되지 않아 BGM 재생을 건너뜁니다.");
            return;
        }

        if (ResourceManager.Instance == null)
        {
            Debug.LogWarning($"[BgmPlayer] ResourceManager가 아직 초기화되지 않아 BGM 재생을 건너뜁니다: {clipKey}");
            return;
        }

        // 진행 중인 페이드 취소
        CancelCurrentFade();
        CancellationToken linkedCt = CreateLinkedToken(ct);

        // 기존 BGM 페이드 아웃
        if (_source.isPlaying && config.FadeOutDuration > 0f)
        {
            await FadeVolumeAsync(_source.volume, 0f, config.FadeOutDuration, linkedCt);
        }
        _source.Stop();

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

        float targetVolume = _baseVolume * config.VolumeMultiplier;
        await FadeVolumeAsync(0f, targetVolume, config.FadeInDuration, linkedCt);
    }

    // [확장] 인트로/루프 분리 재생이 필요하면 CrossfadeWithIntroAsync 구현 추가
    // 인트로 클립 1회 재생 → 루프 클립 무한 반복 패턴

    // 페이드 아웃 후 정지한다
    public async UniTask StopAsync(float fadeDuration, CancellationToken ct = default)
    {
        CancelCurrentFade();
        CancellationToken linkedCt = CreateLinkedToken(ct);

        await FadeVolumeAsync(_source.volume, 0f, fadeDuration, linkedCt);
        _source.Stop();
        _source.clip = null;
        _source.loop = true;
    }

    // 즉시 정지한다
    public void Stop()
    {
        CancelCurrentFade();
        _source.Stop();
        _source.clip = null;
        _source.loop = true;
        _baseVolume = DefaultBgmBaseVolume;
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

    private async UniTask FadeVolumeAsync(float from, float to, float duration, CancellationToken ct)
    {
        if (duration <= 0f)
        {
            _source.volume = to;
            return;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            ct.ThrowIfCancellationRequested();
            elapsed += Time.unscaledDeltaTime;
            _source.volume = Mathf.Lerp(from, to, elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        _source.volume = to;
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

    private CancellationToken CreateLinkedToken(CancellationToken external)
    {
        if (external == default) return _fadeCts.Token;
        return CancellationTokenSource.CreateLinkedTokenSource(_fadeCts.Token, external).Token;
    }

    private void OnDestroy()
    {
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
    }
}
