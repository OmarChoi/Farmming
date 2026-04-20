using System;
using UnityEngine;

/// <summary>
/// 게임 전반의 사용자 설정을 보관하는 싱글톤 매니저.
/// GameSettings를 단일 진실 공급원으로 소유하며, 값이 바뀌면 구독자(SoundManager 등)에게 이벤트로 전파한다.
/// 저장(Save)은 팝업을 닫을 때 한 번만 호출되어 PlayerPrefs 쓰기 빈도를 낮춘다.
///
/// 배치: Title 씬(YJ_StartScene)에 SettingManager.prefab으로 1회 배치하고 DontDestroyOnLoad로 유지한다.
/// SoundManager보다 먼저 Awake가 돌도록 프리팹 배치/Script Execution Order로 보장한다.
/// </summary>
public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }

    // 현재 전체 설정 스냅샷. 외부에서는 Volume 프로퍼티를 통해서만 접근한다.
    private GameSetting _gameSettings;
    private ISettingRepository _repository;

    // 볼륨 값이 변경될 때마다 호출되는 이벤트. SoundManager가 구독해 실시간으로 믹서에 반영한다.
    public event Action<VolumeSettings> OnVolumeChanged;

    // 채널별 볼륨 스냅샷을 노출. UI 초기화 / 다른 소비자 질의에 사용된다.
    public VolumeSettings Volume => _gameSettings.Volume;

    private void Awake()
    {
        // 중복 인스턴스 방지. 이미 다른 씬에서 생성된 인스턴스가 DontDestroyOnLoad로 살아 있으면
        // 새로 로드된 사본은 즉시 폐기한다.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _repository = new LocalSettingRepository();
        _gameSettings = _repository.Load();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #region Volume API

    // 지정 채널의 볼륨을 변경한다.
    // VolumeSettings.WithVolume이 자체적으로 0~1 클램핑을 수행하므로 입력값 검증은 내부에 위임한다.
    // 저장은 여기서 수행하지 않고 이벤트만 발행해, 연속 슬라이더 드래그에서 디스크 I/O가 폭주하는 것을 막는다.
    public void SetVolume(EAudioChannel channel, float value)
    {
        VolumeSettings next = _gameSettings.Volume.WithVolume(channel, value);

        // 동일 값 재설정 시 이벤트를 발화하지 않아 구독자가 불필요하게 동작하는 것을 방지한다.
        if (next.Equals(_gameSettings.Volume)) return;

        _gameSettings.Volume = next;
        OnVolumeChanged?.Invoke(next);
    }

    #endregion

    #region Persistence

    // 현재 설정을 영속 저장소에 커밋한다. UI_Setting이 닫힐 때 호출해 1회만 디스크에 기록한다.
    public void Save()
    {
        _repository.Save(_gameSettings);
    }

    #endregion
}
