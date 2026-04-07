/// <summary>
/// SFX 재생을 담당하는 인터페이스.
/// Infrastructure 레이어에서 ObjectPool + AudioSource 기반으로 구현된다.
/// </summary>
public interface ISfxPlayer
{
    void Play(SfxPlayRequest request);
}
