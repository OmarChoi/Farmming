using System;

/// <summary>
/// 게임 전체 사용자 설정을 묶는 데이터 컨테이너.
/// 현재는 사운드 볼륨만 포함하지만, 향후 Graphics/Gameplay 등 섹션이 필드로 추가될 예정이다.
/// JsonUtility로 직렬화 가능하도록 [Serializable] 클래스로 유지한다.
/// </summary>
[Serializable]
public class GameSetting
{
    // 채널별 볼륨 설정. 구조체 VolumeSettings 자체가 불변 값 객체이므로
    // 변경 시에는 WithVolume으로 새 인스턴스를 만들어 재할당한다.
    public VolumeSettings Volume;

    public GameSetting()
    {
        Volume = VolumeSettings.Default;
    }

    public GameSetting(VolumeSettings volume)
    {
        Volume = volume;
    }
}
