using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>
/// BGM 재생/전환/정지를 담당하는 인터페이스.
/// Infrastructure 레이어에서 AudioSource 기반으로 구현된다.
/// </summary>
public interface IBgmPlayer
{
    // 크로스페이드 전환한다
    UniTask CrossfadeAsync(string clipKey, BgmTransitionConfig config, CancellationToken ct = default);

    // [확장] 인트로/루프 분리 재생이 필요하면 아래 시그니처 추가
    // UniTask CrossfadeWithIntroAsync(string introKey, string loopKey, BgmTransitionConfig config, CancellationToken ct = default);

    // 페이드 아웃 후 정지한다
    UniTask StopAsync(float fadeDuration, CancellationToken ct = default);

    // 즉시 정지한다
    void Stop();

    // 일시 정지한다
    void Pause();

    // 일시 정지를 해제한다
    void Resume();
}
