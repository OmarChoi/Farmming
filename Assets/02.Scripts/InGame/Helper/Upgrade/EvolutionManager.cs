using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class EvolutionManager : MonoBehaviour
{
    private static readonly int AnimHash = Animator.StringToHash("animation");
    private static readonly string[] CutsceneHiddenTransformNameTokens =
    {
        "mouthpoint",
        "effectspawnpoint",
        "spawnpoint",
        "seedbubble",
        "bubbleimage",
        "seedicon",
        "seedcounttext"
    };

    [Header("Studio")]
    [SerializeField] private Transform _studioAnchor;
    [SerializeField] private Transform _backgroundRoot;
    [SerializeField] private Transform _beforeModelRoot;
    [SerializeField] private Transform _afterModelRoot;
    [SerializeField] private Transform _orbitPivot;
    [SerializeField] private SpriteRenderer _backgroundRenderer;
    [SerializeField] private string _focusLayerName = "Evolution_Focus";
    [SerializeField] private Vector3 _beforeLocalEuler;
    [SerializeField] private Vector3 _afterLocalEuler;
    [SerializeField] private UnityEngine.Video.VideoPlayer _backgroundVideoPlayer;

    [Header("Timeline")]
    [SerializeField] private PlayableDirector _director;

    [Header("Camera")]
    [SerializeField] private Camera _baseCamera;
    [SerializeField] private Camera _evolutionOverlayCamera;
    [SerializeField] private CinemachineCamera _evolutionCinemachine;
    [SerializeField] private EvolutionTimelineCameraController _timelineCameraController;
    [SerializeField] private EvolutionTimelineRotationController _timelineRotationController;
    [SerializeField] private EvolutionTimelineEnergyRiseController _timelineEnergyRiseController;
    [SerializeField] private int _cutsceneCameraPriority = 100;

    [Header("White Flash")]
    [SerializeField] private CanvasGroup _whiteFlashCanvasGroup;
    [SerializeField] private Color _whiteFlashColor = Color.white;
    [SerializeField] private float _whiteFlashFadeIn = 0.06f;
    [SerializeField] private float _whiteFlashHold = 0.03f;
    [SerializeField] private float _whiteFlashFadeOut = 0.18f;

    [Header("Galaxy Overlay")]
    [SerializeField] private GalaxyOverlay _galaxyOverlay;

    [Header("Sound")]
    [SerializeField] private float _fieldBgmMuteFadeDuration = 0.25f;
    [SerializeField] private float _fieldBgmRestoreFadeDuration = 0.8f;
    [SerializeField] private float _evolutionBgmFadeInDuration = 0.25f;
    [SerializeField] private float _evolutionBgmFadeOutDuration = 0.6f;
    [SerializeField, Range(0f, 2f)] private float _evolutionBgmVolumeMultiplier = 1f;
    [SerializeField] private float _evolutionSfxLeadTime = 2f;
    [SerializeField] private float _evolutionSfxFallbackDuration = 3f;

    [Header("Lighting Isolation")]
    [SerializeField] private bool _isolateLighting = true;
    [SerializeField] private Light[] _evolutionLights;
    [SerializeField] private AmbientMode _cutsceneAmbientMode = AmbientMode.Flat;
    [SerializeField] private Color _cutsceneAmbientColor = Color.white;
    [SerializeField, Range(0f, 8f)] private float _cutsceneAmbientIntensity = 1f;
    [SerializeField] private bool _cutsceneFogEnabled;
    [SerializeField] private Color _cutsceneFogColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float _cutsceneFogDensity;
    [SerializeField] private Material _cutsceneSkybox;

    [Header("Preview Render Safety")]
    [SerializeField] private bool _logPreviewRenderDiagnostics = true;
    [SerializeField] private bool _replaceInvalidPreviewMaterials = true;
    [SerializeField] private Material _previewFallbackMaterial;

    public event Action<HelperDataSO> OnEvolutionFinished;
    public bool IsPlaying => _currentData != null;

    private HelperDataSO _currentData;
    private HelperEvolutionProfileSO _currentProfile;
    private GameObject _beforeInstance;
    private GameObject _afterInstance;
    private Action<bool> _onFinished;
    private Action _onEvolvedModelShown;
    private readonly List<(Light light, int cullingMask, bool enabled)> _lightCache = new();
    private bool _wasOverlayEnabled;
    private bool _wasStacked;
    private int _previousPriority;
    private bool _hasBaseCameraCullingMaskCache;
    private int _cachedBaseCameraCullingMask;
    private Camera _cachedBaseCameraForCullingMask;
    private float _currentSpinSpeed;
    private Coroutine _spinRoutine;
    private Coroutine _whiteFlashRoutine;
    private Coroutine _fallbackRoutine;
    private Coroutine _evolutionSfxRoutine;
    private Coroutine _evolvedModelShownRoutine;
    private float _evolutionSfxEndRealtime;
    private bool _evolutionSfxPlayed;
    private bool _evolvedModelShownNotified;
    private bool _hasRenderSettingsCache;
    private AmbientMode _cachedAmbientMode;
    private Color _cachedAmbientLight;
    private float _cachedAmbientIntensity;
    private bool _cachedFogEnabled;
    private Color _cachedFogColor;
    private float _cachedFogDensity;
    private Material _cachedSkybox;
    private Material _runtimePreviewFallbackMaterial;

    private int FocusLayer => LayerMask.NameToLayer(_focusLayerName);

    private void Awake()
    {
        if (_timelineCameraController == null)
            _timelineCameraController = GetComponent<EvolutionTimelineCameraController>();
        if (_timelineCameraController == null)
            _timelineCameraController = gameObject.AddComponent<EvolutionTimelineCameraController>();
        if (_timelineRotationController == null)
            _timelineRotationController = GetComponent<EvolutionTimelineRotationController>();
        if (_timelineRotationController == null)
            _timelineRotationController = gameObject.AddComponent<EvolutionTimelineRotationController>();
        if (_timelineEnergyRiseController == null)
            _timelineEnergyRiseController = GetComponent<EvolutionTimelineEnergyRiseController>();
        if (_timelineEnergyRiseController == null)
            _timelineEnergyRiseController = gameObject.AddComponent<EvolutionTimelineEnergyRiseController>();

        EnsureWhiteFlashOverlay();
        if (_backgroundRenderer != null)
            _backgroundRenderer.enabled = _backgroundRenderer.sprite != null;
        SetRoot(_beforeModelRoot, false);
        SetRoot(_afterModelRoot, false);
    }

    private void OnDisable()
    {
        if (_director != null)
            _director.stopped -= OnDirectorStopped;

        DisableEvolutionLighting();
    }

    private void OnDestroy()
    {
        if (_runtimePreviewFallbackMaterial != null)
            Destroy(_runtimePreviewFallbackMaterial);
    }

    private void LateUpdate()
    {
        if (IsPlaying && _isolateLighting && _hasRenderSettingsCache)
            ApplyCutsceneRenderSettings();
    }

    public bool BeginEvolution(
        HelperDataSO data,
        EHelperGrade currentGrade,
        HelperController liveHelper,
        Action<bool> onFinished = null,
        Action onEvolvedModelShown = null)
    {
        if (IsPlaying)
        {
            LogBeginEvolutionFailure(data, currentGrade, "another evolution cutscene is already playing");
            return false;
        }

        if (data == null)
        {
            LogBeginEvolutionFailure(null, currentGrade, "HelperDataSO is null");
            return false;
        }

        if (_studioAnchor == null || _beforeModelRoot == null ||
            _afterModelRoot == null || _orbitPivot == null)
        {
            LogBeginEvolutionFailure(data, currentGrade,
                $"required studio reference is missing. StudioAnchor:{_studioAnchor != null}, BeforeRoot:{_beforeModelRoot != null}, AfterRoot:{_afterModelRoot != null}, OrbitPivot:{_orbitPivot != null}");
            return false;
        }

        GameObject beforePrefab = GetEvolutionPreviewSource(data, currentGrade);
        GameObject afterPrefab = GetEvolutionPreviewSource(data, GetNextGrade(currentGrade));
        if (beforePrefab == null || afterPrefab == null)
        {
            LogBeginEvolutionFailure(data, currentGrade,
                $"preview prefab is missing. BeforePrefab:{beforePrefab != null}, AfterPrefab:{afterPrefab != null}");
            return false;
        }

        if (FocusLayer < 0)
        {
            LogBeginEvolutionFailure(data, currentGrade, $"focus layer '{_focusLayerName}' does not exist");
            return false;
        }

        _currentData = data;
        _currentProfile = data.EvolutionProfile;
        _onFinished = onFinished;
        _onEvolvedModelShown = onEvolvedModelShown;
        _evolvedModelShownNotified = false;

        ApplyProfile(_currentProfile);

        SetRoot(_beforeModelRoot, false);
        SetRoot(_afterModelRoot, false);

        _beforeInstance = Instantiate(beforePrefab, _beforeModelRoot, false);
        _afterInstance = Instantiate(afterPrefab, _afterModelRoot, false);

        PreparePreview(_beforeInstance, _beforeModelRoot, _beforeLocalEuler, true);
        PreparePreview(_afterInstance, _afterModelRoot, _afterLocalEuler, false);
        SetLayerRecursively(_beforeInstance, FocusLayer);
        SetLayerRecursively(_afterInstance, FocusLayer);
        LogEvolutionPreviewRenderState("Before", _beforeInstance);
        LogEvolutionPreviewRenderState("After", _afterInstance);

       // StartEvolutionCutscene 하나만 실행 (내부에서 카메라/스핀/연출 처리)
        StartCoroutine(StartEvolutionCutscene());
        return true;
    }

    private void LogBeginEvolutionFailure(HelperDataSO data, EHelperGrade currentGrade, string reason)
    {
        string helperInfo = data == null
            ? "Helper:null"
            : $"HelperId:{data.HelperId}, HelperName:{data.HelperName}";

        Debug.LogError($"[EvolutionManager] Failed to begin evolution cutscene. {helperInfo}, CurrentGrade:{currentGrade}, Reason:{reason}");
    }

    public void CancelEvolution()
    {
        if (!IsPlaying) return;
        if (_fallbackRoutine != null) StopCoroutine(_fallbackRoutine);
        if (_spinRoutine != null) StopCoroutine(_spinRoutine);
        if (_evolutionSfxRoutine != null) StopCoroutine(_evolutionSfxRoutine);
        if (_evolvedModelShownRoutine != null) StopCoroutine(_evolvedModelShownRoutine);
        if (_timelineCameraController != null) _timelineCameraController.Stop();
        if (_timelineRotationController != null) _timelineRotationController.Stop();
        if (_timelineEnergyRiseController != null) _timelineEnergyRiseController.Stop();
        _spinRoutine = null;
        _evolutionSfxRoutine = null;
        _evolvedModelShownRoutine = null;
        if (_director != null) { _director.stopped -= OnDirectorStopped; _director.Stop(); }
        FinishEvolution(false);
    }

    // Timeline Signal에서 호출
    public void Timeline_PlayWhiteFlash()
    {
        if (_whiteFlashRoutine != null) StopCoroutine(_whiteFlashRoutine);
        _whiteFlashRoutine = StartCoroutine(WhiteFlash());
    }

    // Timeline Signal에서 호출
    public void Timeline_SwapToEvolvedModel()
    {
        if (_afterInstance == null) return;
        SetRoot(_beforeModelRoot, false);
        SetRoot(_afterModelRoot, true);
        _afterInstance.SetActive(true);
        PlayIdle(_afterInstance);
        NotifyEvolvedModelShown();
    }

    public void Timeline_PlayEvolvedIdle() => PlayIdle(_afterInstance);

    private void EnsureInitialTimelineModelVisibility()
    {
        SetRoot(_beforeModelRoot, true);
        SetRoot(_afterModelRoot, false);

        if (_beforeInstance != null)
        {
            _beforeInstance.SetActive(true);
            PlayHappy(_beforeInstance);
        }

        if (_afterInstance != null)
            _afterInstance.SetActive(false);
    }

    private IEnumerator StartEvolutionCutscene()
    {
        // 1. 은하수 페이드 인 (게임 화면 가림)
        if (_galaxyOverlay != null)
            yield return _galaxyOverlay.FadeIn();

        // 2. 카메라 전환 (가려진 순간 조용히 전환)
        EnableEvolutionCamera();
        BeginEvolutionAudio();

        // 4. 은하수 페이드 아웃 (진화 배경 드러남)
        if (_galaxyOverlay != null)
            yield return _galaxyOverlay.FadeOut();

        // 5. Timeline or Fallback 연출 시작
        if (_director != null && _director.playableAsset != null)
        {
            StartTimelineCameraController();
            StartTimelineRotationController();
            StartTimelineEnergyRiseController();
            _director.stopped -= OnDirectorStopped;
            _director.stopped += OnDirectorStopped;
            _director.time = 0d;
            _director.Evaluate();
            EnsureInitialTimelineModelVisibility();
            _director.Play();
            StartEvolutionSfxTrigger();
            StartEvolvedModelShownFallbackTrigger();
        }
        else
        {
            float spin = _currentProfile != null ? _currentProfile.IntroSpinSpeed : 90f;
            float introDur = _currentProfile != null ? _currentProfile.IntroDuration : 2f;
            float zoomDur = _currentProfile != null ? _currentProfile.ImpactZoomDuration : 0.35f;
            _spinRoutine = StartCoroutine(SpinLoopEaseIn(spin, introDur));
            _evolutionSfxRoutine = StartCoroutine(PlayEvolutionSfxAfterDelay(Mathf.Max(0f, introDur + zoomDur - _evolutionSfxLeadTime)));
            _fallbackRoutine = StartCoroutine(FallbackEvolution());
        }
    }

    private void OnDirectorStopped(PlayableDirector _) => FinishEvolution(true);

    private void FinishEvolution(bool completed)
    {
        if (_spinRoutine != null) { StopCoroutine(_spinRoutine); _spinRoutine = null; }
        if (_timelineCameraController != null) _timelineCameraController.Stop();
        if (_timelineRotationController != null) _timelineRotationController.Stop();
        if (_timelineEnergyRiseController != null) _timelineEnergyRiseController.Stop();
        if (_whiteFlashRoutine != null) { StopCoroutine(_whiteFlashRoutine); _whiteFlashRoutine = null; }
        if (_evolutionSfxRoutine != null) { StopCoroutine(_evolutionSfxRoutine); _evolutionSfxRoutine = null; }
        if (_evolvedModelShownRoutine != null) { StopCoroutine(_evolvedModelShownRoutine); _evolvedModelShownRoutine = null; }
        if (_whiteFlashCanvasGroup != null)
        {
            _whiteFlashCanvasGroup.alpha = 0f;
            _whiteFlashCanvasGroup.transform.parent.gameObject.SetActive(false);
        }

        StartCoroutine(EndEvolutionCutscene(completed));
    }

    private IEnumerator EndEvolutionCutscene(bool completed)
    {
        // 1. 은하수 페이드 인 (컷씬 가림)
        if (_galaxyOverlay != null)
            yield return _galaxyOverlay.FadeIn();

        // 2. 카메라 복귀 + 정리 (가려진 순간 조용히 복귀)
        DisableEvolutionCamera();
        CleanupInstances();

        // 3. 은하수 페이드 아웃 (게임 화면 드러남)
        if (_galaxyOverlay != null)
            yield return _galaxyOverlay.FadeOut();

        yield return RestoreEvolutionAudio();

        // 4. 상태 초기화 후 콜백
        HelperDataSO finished = _currentData;
        Action<bool> callback = _onFinished;
        _currentData = null;
        _currentProfile = null;
        _onFinished = null;
        _onEvolvedModelShown = null;
        _evolvedModelShownNotified = false;

        callback?.Invoke(completed);
        if (finished != null) OnEvolutionFinished?.Invoke(finished);
    }

    private void BeginEvolutionAudio()
    {
        if (SoundManager.Instance == null)
            return;

        _evolutionSfxEndRealtime = 0f;
        _evolutionSfxPlayed = false;

        SoundManager.Instance.DuckMainBgm(_fieldBgmMuteFadeDuration);
        SoundManager.Instance.PlayOverlayBgm(
            AssetKey.BGM.HelperEvolution,
            new BgmTransitionConfig(0f, _evolutionBgmFadeInDuration, _evolutionBgmVolumeMultiplier));
    }

    private IEnumerator RestoreEvolutionAudio()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopOverlayBgm(_evolutionBgmFadeOutDuration);

        float sfxRemaining = Mathf.Max(0f, _evolutionSfxEndRealtime - Time.realtimeSinceStartup);
        float waitBeforeRestore = Mathf.Max(_evolutionBgmFadeOutDuration, sfxRemaining);
        if (waitBeforeRestore > 0f)
            yield return new WaitForSecondsRealtime(waitBeforeRestore);

        if (SoundManager.Instance != null)
            SoundManager.Instance.RestoreMainBgm(_fieldBgmRestoreFadeDuration);

        if (_fieldBgmRestoreFadeDuration > 0f)
            yield return new WaitForSecondsRealtime(_fieldBgmRestoreFadeDuration);

        _evolutionSfxEndRealtime = 0f;
        _evolutionSfxPlayed = false;
    }

    private void StartEvolutionSfxTrigger()
    {
        if (_evolutionSfxRoutine != null)
            StopCoroutine(_evolutionSfxRoutine);

        _evolutionSfxRoutine = StartCoroutine(PlayEvolutionSfxAtTimelineTime(GetEvolutionSfxTriggerTime()));
    }

    private void StartEvolvedModelShownFallbackTrigger()
    {
        if (_evolvedModelShownRoutine != null)
            StopCoroutine(_evolvedModelShownRoutine);

        _evolvedModelShownRoutine = StartCoroutine(NotifyEvolvedModelShownAtTimelineTime(GetModelSwapTime()));
    }

    private float GetModelSwapTime()
    {
        return _currentProfile != null ? Mathf.Max(0f, _currentProfile.ModelSwapTime) : 0f;
    }

    private float GetEvolutionSfxTriggerTime()
    {
        float swapTime = _currentProfile != null ? _currentProfile.ModelSwapTime : 0f;
        return Mathf.Max(0f, swapTime - _evolutionSfxLeadTime);
    }

    private IEnumerator NotifyEvolvedModelShownAtTimelineTime(float triggerTime)
    {
        while (IsPlaying && _director != null && _director.playableAsset != null && _director.time < triggerTime)
            yield return null;

        if (IsPlaying && _afterInstance != null &&
            (_afterModelRoot == null || !_afterModelRoot.gameObject.activeInHierarchy || !_afterInstance.activeSelf))
        {
            Timeline_SwapToEvolvedModel();
        }
        else
        {
            NotifyEvolvedModelShown();
        }

        _evolvedModelShownRoutine = null;
    }

    private void NotifyEvolvedModelShown()
    {
        if (_evolvedModelShownNotified)
            return;

        _evolvedModelShownNotified = true;
        _onEvolvedModelShown?.Invoke();
    }

    private IEnumerator PlayEvolutionSfxAtTimelineTime(float triggerTime)
    {
        while (IsPlaying && _director != null && _director.playableAsset != null && _director.time < triggerTime)
            yield return null;

        PlayEvolutionSfx();
        _evolutionSfxRoutine = null;
    }

    private IEnumerator PlayEvolutionSfxAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        PlayEvolutionSfx();
        _evolutionSfxRoutine = null;
    }

    private void PlayEvolutionSfx()
    {
        if (_evolutionSfxPlayed || SoundManager.Instance == null)
            return;

        _evolutionSfxPlayed = true;
        _evolutionSfxEndRealtime = Mathf.Max(
            _evolutionSfxEndRealtime,
            Time.realtimeSinceStartup + Mathf.Max(0f, _evolutionSfxFallbackDuration));

        SoundManager.Instance.PlaySfx(new SfxPlayRequest(
            clipKey: AssetKey.SFX.HelperEvolution,
            spatialMode: ESpatialMode.Flat2D));

        TrackEvolutionSfxLengthAsync().Forget();
    }

    private async UniTaskVoid TrackEvolutionSfxLengthAsync()
    {
        if (ResourceManager.Instance == null)
            return;

        AudioClip clip = await ResourceManager.Instance.LoadAsync<AudioClip>(AssetKey.SFX.HelperEvolution);
        if (clip == null)
            return;

        _evolutionSfxEndRealtime = Mathf.Max(
            _evolutionSfxEndRealtime,
            Time.realtimeSinceStartup + clip.length);
    }

    private void EnableEvolutionCamera()
    {
        if (_baseCamera == null) _baseCamera = Camera.main;
        if (_evolutionOverlayCamera == null)
        {
            Debug.LogError("[EvolutionManager] Evolution overlay camera is missing. Evolution cutscene can play but will not be visible.");
            return;
        }

        _wasOverlayEnabled = _evolutionOverlayCamera.enabled;
        _evolutionOverlayCamera.enabled = true;
        _evolutionOverlayCamera.cullingMask = FocusLayer >= 0 ? 1 << FocusLayer : 0;
        EnableEvolutionLighting();

        if (_baseCamera != null)
        {
            IncludeFocusLayerInBaseCameraCullingMask(_baseCamera);

            UniversalAdditionalCameraData baseData = _baseCamera.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData overlayData = _evolutionOverlayCamera.GetUniversalAdditionalCameraData();
            if (overlayData != null)
            {
                overlayData.renderType = CameraRenderType.Overlay;
            }
            else
            {
                Debug.LogError("[EvolutionManager] Evolution overlay camera has no UniversalAdditionalCameraData. URP camera stack may not render the evolution models.");
            }

            if (baseData != null)
            {
                _wasStacked = baseData.cameraStack.Contains(_evolutionOverlayCamera);
                if (!_wasStacked) baseData.cameraStack.Add(_evolutionOverlayCamera);

                if (_logPreviewRenderDiagnostics)
                {
                    Debug.Log(
                        $"[EvolutionManager] Evolution camera stack state. " +
                        $"BaseCamera:{_baseCamera.name}, OverlayCamera:{_evolutionOverlayCamera.name}, " +
                        $"OverlayType:{(overlayData != null ? overlayData.renderType.ToString() : "MissingURPData")}, " +
                        $"StackContains:{baseData.cameraStack.Contains(_evolutionOverlayCamera)}, " +
                        $"OverlayCullingMask:{_evolutionOverlayCamera.cullingMask}, FocusLayer:{FocusLayer}");
                }
            }
            else
            {
                Debug.LogError("[EvolutionManager] Base camera has no UniversalAdditionalCameraData. Evolution overlay camera cannot be stacked in URP.");
            }
        }
        else
        {
            Debug.LogError("[EvolutionManager] Base camera is missing and Camera.main could not be found. Evolution overlay camera cannot be added to the URP camera stack.");
        }

        if (_evolutionCinemachine != null)
        {
            _previousPriority = _evolutionCinemachine.Priority;
            _evolutionCinemachine.Priority = _cutsceneCameraPriority;
            _evolutionCinemachine.Follow = _orbitPivot;
            _evolutionCinemachine.LookAt = _orbitPivot;
        }
    }

    private void DisableEvolutionCamera()
    {
        DisableEvolutionLighting();
        RestoreBaseCameraCullingMask();

        if (_evolutionOverlayCamera != null)
        {
            if (_baseCamera != null)
            {
                UniversalAdditionalCameraData baseData = _baseCamera.GetUniversalAdditionalCameraData();
                if (baseData != null && !_wasStacked)
                    baseData.cameraStack.Remove(_evolutionOverlayCamera);
            }
            _evolutionOverlayCamera.enabled = false;
        }

        if (_evolutionCinemachine != null)
        {
            _evolutionCinemachine.Priority = _previousPriority;
            _evolutionCinemachine.Follow = null;
            _evolutionCinemachine.LookAt = null;
        }
    }

    private void IncludeFocusLayerInBaseCameraCullingMask(Camera baseCamera)
    {
        int focusLayer = FocusLayer;
        if (baseCamera == null || focusLayer < 0)
            return;

        if (!_hasBaseCameraCullingMaskCache || _cachedBaseCameraForCullingMask != baseCamera)
        {
            _cachedBaseCameraForCullingMask = baseCamera;
            _cachedBaseCameraCullingMask = baseCamera.cullingMask;
            _hasBaseCameraCullingMaskCache = true;
        }

        baseCamera.cullingMask |= 1 << focusLayer;
    }

    private void RestoreBaseCameraCullingMask()
    {
        if (!_hasBaseCameraCullingMaskCache || _cachedBaseCameraForCullingMask == null)
            return;

        _cachedBaseCameraForCullingMask.cullingMask = _cachedBaseCameraCullingMask;
        _cachedBaseCameraForCullingMask = null;
        _hasBaseCameraCullingMaskCache = false;
    }

    private void EnableEvolutionLighting()
    {
        if (!_isolateLighting)
            return;

        CacheRenderSettings();
        ApplyCutsceneRenderSettings();
        ApplyEvolutionLights();
    }

    private void DisableEvolutionLighting()
    {
        RestoreEvolutionLights();
        RestoreRenderSettings();
    }

    private void CacheRenderSettings()
    {
        if (_hasRenderSettingsCache)
            return;

        _cachedAmbientMode = RenderSettings.ambientMode;
        _cachedAmbientLight = RenderSettings.ambientLight;
        _cachedAmbientIntensity = RenderSettings.ambientIntensity;
        _cachedFogEnabled = RenderSettings.fog;
        _cachedFogColor = RenderSettings.fogColor;
        _cachedFogDensity = RenderSettings.fogDensity;
        _cachedSkybox = RenderSettings.skybox;
        _hasRenderSettingsCache = true;
    }

    private void ApplyCutsceneRenderSettings()
    {
        RenderSettings.ambientMode = _cutsceneAmbientMode;
        RenderSettings.ambientLight = _cutsceneAmbientColor;
        RenderSettings.ambientIntensity = _cutsceneAmbientIntensity;
        RenderSettings.fog = _cutsceneFogEnabled;
        RenderSettings.fogColor = _cutsceneFogColor;
        RenderSettings.fogDensity = _cutsceneFogDensity;

        if (_cutsceneSkybox != null)
            RenderSettings.skybox = _cutsceneSkybox;
    }

    private void RestoreRenderSettings()
    {
        if (!_hasRenderSettingsCache)
            return;

        RenderSettings.ambientMode = _cachedAmbientMode;
        RenderSettings.ambientLight = _cachedAmbientLight;
        RenderSettings.ambientIntensity = _cachedAmbientIntensity;
        RenderSettings.fog = _cachedFogEnabled;
        RenderSettings.fogColor = _cachedFogColor;
        RenderSettings.fogDensity = _cachedFogDensity;
        RenderSettings.skybox = _cachedSkybox;
        _hasRenderSettingsCache = false;
    }

    private void ApplyEvolutionLights()
    {
        int focusLayer = FocusLayer;
        if (focusLayer < 0)
            return;

        int focusMask = 1 << focusLayer;
        _lightCache.Clear();

        if (_evolutionLights != null)
        {
            foreach (Light evolutionLight in _evolutionLights)
            {
                if (evolutionLight == null || IsLightCached(evolutionLight))
                    continue;

                _lightCache.Add((evolutionLight, evolutionLight.cullingMask, evolutionLight.enabled));
                evolutionLight.enabled = true;
                evolutionLight.cullingMask = focusMask;
            }
        }
    }

    private bool IsLightCached(Light target)
    {
        foreach (var entry in _lightCache)
        {
            if (entry.light == target)
                return true;
        }

        return false;
    }

    private void RestoreEvolutionLights()
    {
        foreach ((Light light, int cullingMask, bool enabled) in _lightCache)
        {
            if (light == null)
                continue;

            light.cullingMask = cullingMask;
            light.enabled = enabled;
        }

        _lightCache.Clear();
    }

    private void StartTimelineCameraController()
    {
        if (_timelineCameraController == null || _currentProfile == null)
            return;

        Transform cameraTransform = _evolutionCinemachine != null
            ? _evolutionCinemachine.transform
            : _evolutionOverlayCamera != null
                ? _evolutionOverlayCamera.transform
                : null;

        _timelineCameraController.Configure(
            _currentProfile,
            _director,
            cameraTransform,
            _orbitPivot);
        _timelineCameraController.Play();
    }

    private void StartTimelineRotationController()
    {
        if (_timelineRotationController == null || _currentProfile == null)
            return;

        _timelineRotationController.Configure(
            _currentProfile,
            _director,
            _beforeModelRoot,
            _afterModelRoot,
            _beforeLocalEuler,
            _afterLocalEuler);
        _timelineRotationController.Play();
    }

    private void StartTimelineEnergyRiseController()
    {
        if (_timelineEnergyRiseController == null || _currentProfile == null)
            return;

        _timelineEnergyRiseController.Configure(
            _currentProfile,
            _director,
            _beforeModelRoot,
            _afterModelRoot);
        _timelineEnergyRiseController.Play();
    }

    private IEnumerator FallbackEvolution()
    {
        HelperEvolutionProfileSO p = _currentProfile;
        if (_evolutionOverlayCamera == null) { FinishEvolution(true); yield break; }

        Transform cam = _evolutionCinemachine != null
            ? _evolutionCinemachine.transform
            : _evolutionOverlayCamera.transform;

        Vector3 intro = p != null ? p.BeforeIntroCameraLocalPosition : new Vector3(0f, 1.6f, -6f);
        Vector3 zoom = p != null ? p.BeforeImpactZoomCameraLocalPosition : new Vector3(0f, 1.05f, -1.45f);
        Vector3 close = p != null ? p.AfterCloseCameraLocalPosition : new Vector3(0f, 0.45f, -1.35f);
        Vector3 pullback = p != null ? p.AfterPullbackCameraLocalPosition : new Vector3(0f, 0.55f, -2.05f);
        Vector3 head = p != null ? p.AfterHeadCameraLocalPosition : new Vector3(0f, 1.95f, -2.15f);
        Vector3 fullShot = p != null ? p.AfterFullShotCameraLocalPosition : new Vector3(0f, 1.25f, -3.25f);
        float introDur = p != null ? p.IntroDuration : 2f;
        float zoomDur = p != null ? p.ImpactZoomDuration : 0.35f;
        float pullbackDur = p != null ? p.AfterPullbackDuration : 0.45f;
        float scanDur = p != null ? p.AfterScanDuration : 2.4f;
        float outroDur = p != null ? p.FinalShowcaseDuration : 2f;

        // Intro: 카메라 고정 (스핀은 StartEvolutionCutscene에서 이미 시작됨)
        cam.localPosition = intro;
        float t = 0f;
        while (t < introDur) { t += Time.deltaTime; LookAtPivot(cam); yield return null; }

        // Zoom: 카메라 줌인
        yield return MoveCamera(cam, intro, zoom, zoomDur);

        // Flash + 모델 교체
        Timeline_PlayWhiteFlash();
        yield return new WaitForSeconds(Mathf.Max(_whiteFlashFadeIn * 0.75f, 0.03f));
        Timeline_SwapToEvolvedModel();

        // AfterActive: close -> small pullback -> foot to head scan
        cam.localPosition = close;
        yield return MoveCamera(cam, close, pullback, pullbackDur);
        yield return MoveCamera(cam, pullback, head, scanDur);

        // Outro: 카메라 살짝 뒤로
        yield return MoveCamera(cam, head, fullShot, outroDur);

        if (_spinRoutine != null) { StopCoroutine(_spinRoutine); _spinRoutine = null; }
        _fallbackRoutine = null;
        FinishEvolution(true);
    }
    private IEnumerator SpinLoopEaseIn(float spinSpeed, float duration)
    {
        float t = 0f;
        // duration 동안 점점 빨라짐
        while (t < duration)
        {
            t += Time.deltaTime;
            float eased = Mathf.SmoothStep(0f, 1f, t / duration);
            _currentSpinSpeed = spinSpeed * eased;
            RotateVisibleModel(_currentSpinSpeed * Time.deltaTime);
            yield return null;
        }
        // 이후 최대 속도로 계속 회전
        while (true)
        {
            _currentSpinSpeed = spinSpeed;
            RotateVisibleModel(spinSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator SpinLoopEaseOut(float spinSpeed, float duration, Transform target, float targetY)
    {
        if (target == null) yield break;

        float startY = target.localEulerAngles.y;

        // Before에서 이어받은 속도에서 시작, 최소 2바퀴 보장
        float totalRotation = spinSpeed * duration * 0.5f;
        float totalAngle = totalRotation;

        float remainder = (startY + totalAngle - targetY) % 360f;
        totalAngle -= remainder;
        while (Mathf.Abs(totalAngle) < 720f)
            totalAngle += totalAngle >= 0 ? 360f : -360f;

        float startSpeed = _currentSpinSpeed > 0 ? _currentSpinSpeed : spinSpeed;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float currentSpeed = Mathf.Lerp(startSpeed, 0f, Mathf.SmoothStep(0f, 1f, progress));
            _currentSpinSpeed = currentSpeed;

            float angle = Mathf.Lerp(0f, totalAngle, Mathf.SmoothStep(0f, 1f, progress));
            Vector3 e = target.localEulerAngles;
            target.localEulerAngles = new Vector3(e.x, startY + angle, e.z);
            yield return null;
        }

        _currentSpinSpeed = 0f;
        Vector3 fin = target.localEulerAngles;
        target.localEulerAngles = new Vector3(fin.x, targetY, fin.z);
    }

    private IEnumerator MoveCamera(Transform cam, Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            cam.localPosition = Vector3.Lerp(
                from, to,
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration)));
            LookAtPivot(cam);
            yield return null;
        }
        cam.localPosition = to;
        LookAtPivot(cam);
    }

    private void RotateVisibleModel(float yaw)
    {
        if (_beforeModelRoot != null && _beforeModelRoot.gameObject.activeSelf)
            _beforeModelRoot.Rotate(0f, yaw, 0f);
        else if (_afterModelRoot != null && _afterModelRoot.gameObject.activeSelf)
            _afterModelRoot.Rotate(0f, yaw, 0f);
    }

    private void LookAtPivot(Transform cam)
    {
        if (cam == null || _orbitPivot == null) return;
        Vector3 dir = _orbitPivot.position - cam.position;
        if (dir.sqrMagnitude > 0.0001f)
            cam.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    private IEnumerator WhiteFlash()
    {
        if (_whiteFlashCanvasGroup == null) yield break;
        GameObject root = _whiteFlashCanvasGroup.transform.parent.gameObject;
        Image image = _whiteFlashCanvasGroup.GetComponent<Image>();
        if (image != null) image.color = _whiteFlashColor;
        root.SetActive(true);
        yield return FadeFlash(0f, 1f, _whiteFlashFadeIn);
        if (_whiteFlashHold > 0f) yield return new WaitForSeconds(_whiteFlashHold);
        yield return FadeFlash(1f, 0f, _whiteFlashFadeOut);
        root.SetActive(false);
        _whiteFlashRoutine = null;
    }

    private IEnumerator FadeFlash(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _whiteFlashCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        _whiteFlashCanvasGroup.alpha = to;
    }

    private void ApplyProfile(HelperEvolutionProfileSO profile)
    {
        if (profile == null) return;
        _beforeLocalEuler = profile.BeforeLocalEuler;
        _afterLocalEuler = profile.AfterLocalEuler;
        _whiteFlashColor = profile.FlashColor;
        if (_director != null) _director.playableAsset = profile.TimelineAsset;
        if (_orbitPivot != null)
            _orbitPivot.localPosition = new Vector3(0f, profile.PivotHeight, 0f);
        /*        if (_backgroundRenderer != null)
                {
                    _backgroundRenderer.sprite = profile.BackgroundSprite;
                    _backgroundRenderer.enabled = profile.BackgroundSprite != null;
                }*/
        if (_backgroundVideoPlayer != null)
        {
            if (profile.BackgroundVideo != null)
            {
                _backgroundVideoPlayer.clip = profile.BackgroundVideo;
                _backgroundVideoPlayer.enabled = true;
                _backgroundVideoPlayer.Play();
            }
            else
            {
                _backgroundVideoPlayer.Stop();
                _backgroundVideoPlayer.enabled = false;
            }
        }

    }

    private void PreparePreview(GameObject instance, Transform parent, Vector3 localEuler, bool active)
    {
        if (instance == null || parent == null) return;

        // SetParent 전에 비활성화 → OnDisable 충돌 방지
        instance.SetActive(false);

        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.Euler(localEuler);
        instance.transform.localScale = Vector3.one;

        PrepareCutsceneOnlyModel(instance);

        foreach (Behaviour behaviour in instance.GetComponentsInChildren<Behaviour>(true))
        {
            if (behaviour == null || behaviour is Animator) continue;
            if (false)
            {
                // live helper 상태는 CacheAndDisableLiveState에서 별도 처리
            }
            behaviour.enabled = false;
        }
        instance.SetActive(active);
        if (active) PlayIdle(instance);
    }

    private void PrepareCutsceneOnlyModel(GameObject root)
    {
        if (root == null) return;

        SetLayerRecursively(root, FocusLayer);

        foreach (Behaviour behaviour in root.GetComponentsInChildren<Behaviour>(true))
        {
            if (behaviour == null || behaviour is Animator)
                continue;

            behaviour.enabled = false;
        }

        foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
            PrepareAnimator(animator);

        HideCutsceneMarkerRenderers(root);

        foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            renderer.updateWhenOffscreen = true;

        EnsurePreviewRenderersVisible(root);

        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        foreach (Rigidbody rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
        {
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;
        }
    }

    private static void HideCutsceneMarkerRenderers(GameObject root)
    {
        if (root == null)
            return;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null && IsCutsceneHiddenTransform(renderer.transform))
                renderer.enabled = false;
        }
    }

    private static bool IsCutsceneHiddenTransform(Transform transform)
    {
        while (transform != null)
        {
            foreach (string token in CutsceneHiddenTransformNameTokens)
            {
                if (transform.name.Contains(token, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            transform = transform.parent;
        }

        return false;
    }

    private void EnsurePreviewRenderersVisible(GameObject root)
    {
        if (root == null)
            return;

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || IsCutsceneHiddenTransform(renderer.transform))
                continue;

            renderer.enabled = true;
            renderer.forceRenderingOff = false;
            renderer.shadowCastingMode = ShadowCastingMode.On;

            if (renderer is SkinnedMeshRenderer skinnedRenderer)
                skinnedRenderer.updateWhenOffscreen = true;

            if (_replaceInvalidPreviewMaterials)
                ReplaceInvalidPreviewMaterials(renderer);
        }
    }

    private void ReplaceInvalidPreviewMaterials(Renderer renderer)
    {
        if (renderer == null)
            return;

        Material[] materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
            return;

        bool changed = false;
        for (int i = 0; i < materials.Length; i++)
        {
            if (!IsInvalidPreviewMaterial(materials[i]))
                continue;

            Material fallback = GetPreviewFallbackMaterial(materials[i]);
            if (fallback == null)
                continue;

            materials[i] = fallback;
            changed = true;
        }

        if (changed)
            renderer.sharedMaterials = materials;
    }

    private static bool IsInvalidPreviewMaterial(Material material)
    {
        if (material == null)
            return true;

        Shader shader = material.shader;
        if (shader == null || !shader.isSupported)
            return true;

        return string.Equals(shader.name, "Hidden/InternalErrorShader", StringComparison.Ordinal);
    }

    private Material GetPreviewFallbackMaterial(Material source)
    {
        if (_previewFallbackMaterial != null)
            return _previewFallbackMaterial;

        if (_runtimePreviewFallbackMaterial != null)
            return _runtimePreviewFallbackMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
            return null;

        _runtimePreviewFallbackMaterial = new Material(shader)
        {
            name = "Runtime_EvolutionPreviewFallback"
        };

        CopyPreviewMaterialVisuals(source, _runtimePreviewFallbackMaterial);
        return _runtimePreviewFallbackMaterial;
    }

    private static void CopyPreviewMaterialVisuals(Material source, Material target)
    {
        if (source == null || target == null)
            return;

        if (source.HasProperty("_BaseColor") && target.HasProperty("_BaseColor"))
            target.SetColor("_BaseColor", source.GetColor("_BaseColor"));
        else if (source.HasProperty("_Color"))
        {
            if (target.HasProperty("_BaseColor"))
                target.SetColor("_BaseColor", source.GetColor("_Color"));
            if (target.HasProperty("_Color"))
                target.SetColor("_Color", source.GetColor("_Color"));
        }

        Texture texture = null;
        if (source.HasProperty("_BaseMap"))
            texture = source.GetTexture("_BaseMap");
        if (texture == null && source.HasProperty("_MainTex"))
            texture = source.GetTexture("_MainTex");

        if (texture == null)
            return;

        if (target.HasProperty("_BaseMap"))
            target.SetTexture("_BaseMap", texture);
        if (target.HasProperty("_MainTex"))
            target.SetTexture("_MainTex", texture);
    }

    private void LogEvolutionPreviewRenderState(string label, GameObject instance)
    {
        if (!_logPreviewRenderDiagnostics)
            return;

        if (instance == null)
        {
            Debug.LogError($"[EvolutionManager] {label} preview instance is null.");
            return;
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        int enabledCount = 0;
        int focusLayerCount = 0;
        int invalidMaterialCount = 0;
        int focusLayer = FocusLayer;
        int focusMask = focusLayer >= 0 ? 1 << focusLayer : 0;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || IsCutsceneHiddenTransform(renderer.transform))
                continue;

            if (renderer.enabled && !renderer.forceRenderingOff)
                enabledCount++;

            if (focusLayer >= 0 && renderer.gameObject.layer == focusLayer)
                focusLayerCount++;

            Material[] materials = renderer.sharedMaterials;
            if (materials == null)
                continue;

            for (int i = 0; i < materials.Length; i++)
            {
                if (IsInvalidPreviewMaterial(materials[i]))
                    invalidMaterialCount++;
            }
        }

        string cameraInfo = _evolutionOverlayCamera == null
            ? "OverlayCamera:null"
            : $"OverlayCamera:{_evolutionOverlayCamera.name}, Enabled:{_evolutionOverlayCamera.enabled}, CullingMask:{_evolutionOverlayCamera.cullingMask}, SeesFocus:{(_evolutionOverlayCamera.cullingMask & focusMask) != 0}";

        Debug.Log(
            $"[EvolutionManager] {label} preview render state. " +
            $"Instance:{instance.name}, Active:{instance.activeInHierarchy}, " +
            $"RendererTotal:{renderers.Length}, EnabledRenderable:{enabledCount}, FocusLayerRenderable:{focusLayerCount}, " +
            $"InvalidMaterials:{invalidMaterialCount}, FocusLayer:{focusLayer}, {cameraInfo}");

        if (renderers.Length == 0 || enabledCount == 0 || focusLayerCount == 0 || invalidMaterialCount > 0)
        {
            Debug.LogWarning(
                $"[EvolutionManager] {label} preview may not render correctly in build. " +
                $"RendererTotal:{renderers.Length}, EnabledRenderable:{enabledCount}, FocusLayerRenderable:{focusLayerCount}, InvalidMaterials:{invalidMaterialCount}");
        }
    }

    private void CleanupInstances()
    {
        if (_beforeInstance != null) Destroy(_beforeInstance);

        if (_afterInstance != null) Destroy(_afterInstance);
        SetRoot(_beforeModelRoot, false);
        SetRoot(_afterModelRoot, false);
        if (_backgroundVideoPlayer != null)
        {
            _backgroundVideoPlayer.Stop();
            _backgroundVideoPlayer.enabled = false;
        }
    }

    private void EnsureWhiteFlashOverlay()
    {
        if (_whiteFlashCanvasGroup != null) return;

        GameObject canvasGo = new GameObject("EvolutionWhiteFlashCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject imageGo = new GameObject("WhiteFlash");
        imageGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rect = imageGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = imageGo.AddComponent<Image>();
        image.color = _whiteFlashColor;
        _whiteFlashCanvasGroup = imageGo.AddComponent<CanvasGroup>();
        _whiteFlashCanvasGroup.alpha = 0f;
        canvasGo.SetActive(false);
    }

    private static void PlayIdle(GameObject target)
    {
        PlayHelperAnimation(target, EHelperAnim.Idle);
    }

    private static void PlayHappy(GameObject target)
    {
        PlayHelperAnimation(target, EHelperAnim.Happy);
    }

    private static void PlayHelperAnimation(GameObject target, EHelperAnim animation)
    {
        if (target == null) return;

        Animator animator = target.GetComponentInChildren<Animator>(true);
        if (animator == null) return;
        PrepareAnimator(animator);
        animator.SetInteger(AnimHash, (int)animation);
        animator.Update(0f);
    }

    private static void PrepareAnimator(Animator animator)
    {
        if (animator == null) return;

        animator.enabled = true;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.Rebind();
        animator.Update(0f);
    }

    private static void SetRoot(Transform root, bool active)
    {
        if (root != null) root.gameObject.SetActive(active);
    }

    private static EHelperGrade GetNextGrade(EHelperGrade currentGrade)
    {
        return currentGrade == EHelperGrade.Normal ? EHelperGrade.Epic :
               currentGrade == EHelperGrade.Epic ? EHelperGrade.Legendary :
                                                     EHelperGrade.Legendary;
    }

    private static GameObject GetEvolutionPreviewSource(HelperDataSO data, EHelperGrade grade)
    {
        if (data == null)
            return null;

        GameObject previewPrefab = data.GetEvolutionPreviewPrefab(grade);
        if (previewPrefab != null)
            return previewPrefab;

        HelperController gameplayPrefab = data.GetPrefabForGrade(grade);
        return gameplayPrefab != null ? gameplayPrefab.gameObject : null;
    }

    public void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0) return;
        foreach (Transform child in target.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }
}
