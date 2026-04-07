/// <summary>
/// BGM 전환 시 페이드 타이밍과 재생 방식을 정의하는 불변 값 객체.
/// </summary>
public readonly struct BgmTransitionConfig
{
    public float FadeOutDuration { get; }
    public float FadeInDuration { get; }
    public float VolumeMultiplier { get; }

    public BgmTransitionConfig(float fadeOutDuration, float fadeInDuration, float volumeMultiplier = 1f)
    {
        FadeOutDuration = fadeOutDuration > 0f ? fadeOutDuration : 0f;
        FadeInDuration = fadeInDuration > 0f ? fadeInDuration : 0f;
        VolumeMultiplier = volumeMultiplier > 0f ? volumeMultiplier : 1f;
    }

    // 기본 크로스페이드 설정 (0.5초 아웃, 1초 인)
    public static BgmTransitionConfig Default => new BgmTransitionConfig(0.5f, 1f);
}
