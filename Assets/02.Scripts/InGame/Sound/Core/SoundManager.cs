using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 사운드 시스템의 Application 레이어 파사드.
/// 볼륨 관리, BGM/SFX 재생 요청을 조율하며 Infrastructure에 위임한다.
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

    // Domain
    private VolumeSettings _volumeSettings;

    // Infrastructure
    private IBgmPlayer _bgmPlayer;
    private ISfxPlayer _sfxPlayer;
    private ISoundSettingsRepository _settingsRepository;
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
        LoadVolumeSettings();
        PreloadCommonSounds().Forget();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #region Initialization

    private void InitializeInfrastructure()
    {
        // 볼륨 믹서
        _volumeMixer = new VolumeMixer(_audioMixer);

        // 볼륨 저장소
        _settingsRepository = new LocalSoundSettingsRepository();

        // BGM 플레이어
        BgmPlayer bgmPlayer = gameObject.AddComponent<BgmPlayer>();
        bgmPlayer.Initialize(transform, _musicGroup);
        _bgmPlayer = bgmPlayer;

        // SFX 플레이어
        SfxPlayer sfxPlayer = gameObject.AddComponent<SfxPlayer>();
        sfxPlayer.Initialize(transform, _effectGroup, _sfxPoolSize);
        _sfxPlayer = sfxPlayer;
    }

    private void LoadVolumeSettings()
    {
        _volumeSettings = _settingsRepository.Load();
        _volumeMixer.ApplyAll(_volumeSettings);
    }

    private async UniTaskVoid PreloadCommonSounds()
    {
        if (string.IsNullOrEmpty(_commonSoundLabel)) return;
        await ResourceManager.Instance.LoadAllAsync<AudioClip>(_commonSoundLabel);
    }

    #endregion

    #region Volume Control

    // 지정 채널의 볼륨을 설정한다
    public void SetVolume(EAudioChannel channel, float volume)
    {
        _volumeSettings = _volumeSettings.WithVolume(channel, volume);
        _volumeMixer.ApplyVolume(channel, _volumeSettings.GetVolume(channel));
        _settingsRepository.Save(_volumeSettings);
    }

    // 지정 채널의 볼륨을 조회한다
    public float GetVolume(EAudioChannel channel) => _volumeSettings.GetVolume(channel);
    
    public void MuteAll() => _volumeMixer.Mute();
    public void UnmuteAll() => _volumeMixer.ApplyVolume(EAudioChannel.Master, _volumeSettings.Master);
    #endregion

    #region BGM Control

    // 단일 클립으로 BGM을 크로스페이드 전환한다
    public void CrossfadeBgm(string clipKey, BgmTransitionConfig config) => _bgmPlayer.CrossfadeAsync(clipKey, config).Forget();

    // 기본 설정으로 BGM을 크로스페이드 전환한다
    public void CrossfadeBgm(string clipKey) => _bgmPlayer.CrossfadeAsync(clipKey, BgmTransitionConfig.Default).Forget();

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
