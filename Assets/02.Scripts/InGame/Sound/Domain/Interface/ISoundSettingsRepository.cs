/// <summary>
/// 볼륨 설정의 영속화를 담당하는 리포지토리 인터페이스.
/// 디바이스별 사용자 선호 설정을 저장/로드한다.
/// 다른 저장 데이터와 다르게 별개로 저장할 확률이 높다고 생각해서 분리
/// </summary>
public interface ISoundSettingsRepository
{
    // 현재 볼륨 설정을 저장한다
    public void Save(VolumeSettings settings);

    // 저장된 볼륨 설정을 불러온다 (없으면 기본값 반환)
    public VolumeSettings Load();
}
