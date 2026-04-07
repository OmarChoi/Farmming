using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// AudioMixer의 볼륨 파라미터 제어를 캡슐화하는 클래스.
/// 선형 볼륨(0~1)을 데시벨(dB)로 변환하여 믹서에 적용한다.
/// </summary>
public class VolumeMixer
{
    private readonly AudioMixer _mixer;

    public VolumeMixer(AudioMixer mixer)
    {
        _mixer = mixer;
    }

    // 지정 채널의 볼륨을 믹서에 적용한다
    public void ApplyVolume(EAudioChannel channel, float linearVolume)
    {
        if (_mixer == null) return;

        // 무음 방지를 위한 최솟값 보정 후 dB 변환
        float clamped = Mathf.Max(0.0001f, Mathf.Clamp01(linearVolume));
        float dB = Mathf.Log10(clamped) * 20f;
        _mixer.SetFloat(channel.ToString(), dB);
    }

    // 모든 채널의 볼륨을 일괄 적용한다
    public void ApplyAll(VolumeSettings settings)
    {
        ApplyVolume(EAudioChannel.Master, settings.Master);
        ApplyVolume(EAudioChannel.Music, settings.Music);
        ApplyVolume(EAudioChannel.Effect, settings.Effect);
    }

    // 마스터 채널을 음소거한다 (Pause 용도)
    public void Mute()
    {
        ApplyVolume(EAudioChannel.Master, 0f);
    }
}
