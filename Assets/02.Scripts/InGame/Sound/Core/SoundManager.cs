using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 사운드 시스템의 Application 레이어 파사드.
/// BGM/SFX 재생을 조율하고 VolumeMixer로 실제 믹서 반영을 위임한다.
/// 볼륨 값의 소유권은 SettingManager가 가지며, SoundManager는 OnVolumeChanged 이벤트를 구독해
/// 값을 받아 믹서에 반영만 하는 stateless 소비자로 동작한다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private AudioMixerGroup _musicGroup;
    [SerializeField] private AudioMixerGroup _effectGroup;

    [Header("SFX Pool")]
    [SerializeField] private int _sfxPoolSize = 15;

    [Header("Common Sounds")]
    [SerializeField] private string _commonSoundLabel = "CommonSound";

    // Infrastructure
    private IBgmPlayer _bgmPlayer;
    private ISfxPlayer _sfxPlayer;
    private VolumeMixer _volumeMixer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeInfrastructure();
        PreloadCommonSounds().Forget();
    }

    private void Start()
    {
        BindSettingManager();
    }

    private void OnDestroy()
    {
        // SettingManager가 먼저 파괴될 수도 있으므로 null 체크 후 안전하게 해제한다.
        if (SettingManager.Instance != null)
        {
            SettingManager.Instance.OnVolumeChanged -= ApplyVolumes;
        }

        if (Instance == this) Instance = null;
    }

    #region Initialization

    private void InitializeInfrastructure()
    {
        // 볼륨 믹서 (AudioMixer의 Exposed Parameter를 dB로 변환해 적용)
        _volumeMixer = new VolumeMixer(_audioMixer);

        // BGM 플레이어
        BgmPlayer bgmPlayer = gameObject.AddComponent<BgmPlayer>();
        bgmPlayer.Initialize(transform, _musicGroup);
        _bgmPlayer = bgmPlayer;

        // SFX 플레이어
        SfxPlayer sfxPlayer = gameObject.AddComponent<SfxPlayer>();
        sfxPlayer.Initialize(transform, _effectGroup, _sfxPoolSize);
        _sfxPlayer = sfxPlayer;
    }

    // SettingManager에서 초기 볼륨을 받아 믹서에 반영하고 변경 이벤트를 구독한다.
    // SettingManager는 Title 씬에서 먼저 Awake되어 DontDestroyOnLoad로 유지되므로 여기서 Instance가 반드시 유효해야 한다.
    private void BindSettingManager()
    {
        SettingManager sm = SettingManager.Instance;
        if (sm == null)
        {
            Debug.LogError("[SoundManager] SettingManager.Instance가 존재하지 않는다. Title 씬 경유 없이 진입한 경우이거나 배치 순서 문제이다.");
            return;
        }

        ApplyVolumes(sm.Volume);
        sm.OnVolumeChanged += ApplyVolumes;
    }

    // SettingManager로부터 받은 볼륨 값을 믹서에 일괄 반영한다.
    private void ApplyVolumes(VolumeSettings settings)
    {
        _volumeMixer.ApplyAll(settings);
    }

    private async UniTaskVoid PreloadCommonSounds()
    {
        if (string.IsNullOrEmpty(_commonSoundLabel)) return;
        await ResourceManager.Instance.LoadAllAsync<AudioClip>(_commonSoundLabel);
    }

    #endregion

    #region Mute Control

    // 전체 음소거. 설정 값에는 영향을 주지 않고 믹서만 즉시 음소거한다.
    public void MuteAll() => _volumeMixer.Mute();

    // 음소거 해제. 현재 SettingManager가 보유한 Master 값을 다시 적용한다.
    public void UnmuteAll()
    {
        float master = SettingManager.Instance != null
            ? SettingManager.Instance.Volume.Master
            : VolumeSettings.Default.Master;
        _volumeMixer.ApplyVolume(EAudioChannel.Master, master);
    }

    #endregion

    #region BGM Control

    // 단일 클립으로 BGM을 크로스페이드 전환한다
    public void CrossfadeBgm(string clipKey, BgmTransitionConfig config) => _bgmPlayer.CrossfadeAsync(clipKey, config).Forget();

    // 기본 설정으로 BGM을 크로스페이드 전환한다
    public void CrossfadeBgm(string clipKey) => _bgmPlayer.CrossfadeAsync(clipKey, BgmTransitionConfig.Default).Forget();

    public void DuckMainBgm(float fadeDuration = 0.25f) => _bgmPlayer.DuckMainAsync(fadeDuration).Forget();

    public void RestoreMainBgm(float fadeDuration = 0.8f) => _bgmPlayer.RestoreMainAsync(fadeDuration).Forget();

    public void PlayOverlayBgm(string clipKey, BgmTransitionConfig config) => _bgmPlayer.PlayOverlayAsync(clipKey, config).Forget();

    public void StopOverlayBgm(float fadeDuration = 0.6f) => _bgmPlayer.StopOverlayAsync(fadeDuration).Forget();

    // BGM을 페이드 아웃 후 정지한다
    public void StopBgm(float fadeDuration = 0.5f) => _bgmPlayer.StopAsync(fadeDuration).Forget();

    // BGM을 즉시 정지한다
    public void StopBgmImmediate() => _bgmPlayer.Stop();

    public void PauseBgm() => _bgmPlayer.Pause();
    public void ResumeBgm() => _bgmPlayer.Resume();
    #endregion

    // SFX를 재생한다
    public void PlaySfx(SfxPlayRequest request) => _sfxPlayer.Play(request);
}
