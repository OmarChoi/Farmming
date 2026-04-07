using System;

/// <summary>
/// 채널별 볼륨 값을 보관하는 불변 값 객체.
/// 모든 볼륨은 0~1 범위로 클램핑된다.
/// </summary>
public readonly struct VolumeSettings : IEquatable<VolumeSettings>
{
    public float Master { get; }
    public float Music { get; }
    public float Effect { get; }

    private const float DefaultVolume = 0.5f;

    public VolumeSettings(float master, float music, float effect)
    {
        Master = Clamp01(master);
        Music = Clamp01(music);
        Effect = Clamp01(effect);
    }

    // 기본값으로 초기화된 설정을 반환한다
    public static VolumeSettings Default => new VolumeSettings(DefaultVolume, DefaultVolume, DefaultVolume);

    // 지정 채널의 볼륨을 반환한다
    public float GetVolume(EAudioChannel channel)
    {
        return channel switch
        {
            EAudioChannel.Master => Master,
            EAudioChannel.Music => Music,
            EAudioChannel.Effect => Effect,
            _ => DefaultVolume
        };
    }

    // 지정 채널의 볼륨을 변경한 새 인스턴스를 반환한다 (불변성 유지)
    public VolumeSettings WithVolume(EAudioChannel channel, float value)
    {
        float clamped = Clamp01(value);
        return channel switch
        {
            EAudioChannel.Master => new VolumeSettings(clamped, Music, Effect),
            EAudioChannel.Music => new VolumeSettings(Master, clamped, Effect),
            EAudioChannel.Effect => new VolumeSettings(Master, Music, clamped),
            _ => this
        };
    }

    private static float Clamp01(float value)
    {
        if (value < 0f) return 0f;
        if (value > 1f) return 1f;
        return value;
    }

    public bool Equals(VolumeSettings other)
    {
        return Math.Abs(Master - other.Master) < 0.0001f
            && Math.Abs(Music - other.Music) < 0.0001f
            && Math.Abs(Effect - other.Effect) < 0.0001f;
    }

    public override bool Equals(object obj) => obj is VolumeSettings other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Master, Music, Effect);
}
