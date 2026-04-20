/// <summary>
/// 게임 설정의 영속화를 담당하는 리포지토리 인터페이스.
/// 디바이스별 사용자 선호 설정을 저장/로드하며, 세이브 슬롯과 무관하다.
/// </summary>
public interface ISettingRepository
{
    // 저장된 설정을 로드한다. 저장된 값이 없으면 기본값을 반환해야 한다.
    GameSetting Load();

    // 현재 설정을 영속 저장소에 기록한다.
    void Save(GameSetting gameSettings);
}
