using UnityEngine;

public class SkyboxController : MonoBehaviour
{
    private static readonly int MainCubemapId = Shader.PropertyToID("_Tex");
    private static readonly int BlendCubemapId = Shader.PropertyToID("_Tex_Blend");
    private static readonly int CubemapTransitionId = Shader.PropertyToID("_CubemapTransition");
    private static readonly int FogIntensityId = Shader.PropertyToID("_FogIntensity");
    private static readonly int FogPositionId = Shader.PropertyToID("_FogPosition");
    private static readonly int FogFillId = Shader.PropertyToID("_FogFill");

    [Header("References")]
    [SerializeField] private TimeSettingSO _timeSettings;
    [SerializeField] private Material _skyboxTemplate;
    [SerializeField] private SkyDatabase _skyDatabase;

    private const float GIUpdateThreshold = 0.05f;

    private Material _originalSkybox;
    private Material _runtimeSkyboxMaterial;
    private Cubemap _templateMainCubemap;
    private Cubemap _templateBlendCubemap;
    private float _lastGIBlendValue = -1f;
    private SkyKeyframeSO _lastGIFromSegment;

    #region Lifecycle

    // 런타임 머터리얼 생성 및 이벤트 구독
    private void OnEnable()
    {
        EnsureRuntimeMaterial();
        TimeEvents.OnMinuteChanged += UpdateSkybox;
        RefreshNow();
    }

    // 이벤트 구독 해제 및 원본 Skybox 복원
    private void OnDisable()
    {
        TimeEvents.OnMinuteChanged -= UpdateSkybox;
        RestoreSkybox();
    }

    // 원본 Skybox 복원 및 런타임 머터리얼 해제
    private void OnDestroy()
    {
        RestoreSkybox();

        if (_runtimeSkyboxMaterial != null)
        {
            Destroy(_runtimeSkyboxMaterial);
            _runtimeSkyboxMaterial = null;
        }
    }

    // RenderSettings.skybox가 런타임 머터리얼이면 원본으로 되돌린다
    private void RestoreSkybox()
    {
        if (RenderSettings.skybox == _runtimeSkyboxMaterial)
        {
            RenderSettings.skybox = _originalSkybox;
        }
    }

    #endregion

    #region Public API

    // TimeSystem 초기화 전이면 기본 시간, 이후면 현재 시간으로 Skybox를 즉시 갱신한다
    public void RefreshNow()
    {
        if (_timeSettings == null) return;
        GameTime currentTime = TimeEvents.CurrentDay > 0 ? TimeEvents.CurrentTime : _timeSettings.DefaultTime;
        UpdateSkybox(currentTime);
    }

    #endregion

    #region Skybox Update

    // 현재 시간에 해당하는 세그먼트를 찾아 Skybox와 Fog 파라미터를 블렌딩한다
    // SetTexture/SetFloat : Skybox 시각적 블렌딩
    // GI Update : 간접광 재계산
    private void UpdateSkybox(GameTime time)
    {
        if (!EnsureRuntimeMaterial() || _timeSettings == null || _skyDatabase == null) return;
        if (!SkySegmentResolver.TryResolve(time, _timeSettings, _skyDatabase, out SkyKeyframeSO from, out SkyKeyframeSO to, out float segmentProgress)) return;
        if (from == null || to == null) return;

        // 커브를 적용하여 비선형 전환 속도를 계산
        float skyboxProgress = SkySegmentResolver.EvaluateCurve(from.BlendCurve, segmentProgress);

        // Cubemap이 미할당이면 템플릿 머터리얼의 Cubemap을 대체 사용
        Cubemap fromCubemap = from.Cubemap ?? _templateMainCubemap ?? _templateBlendCubemap;
        Cubemap toCubemap = to.Cubemap ?? _templateBlendCubemap ?? _templateMainCubemap;

        // Skybox 블렌딩 및 Fog 파라미터 적용
        _runtimeSkyboxMaterial.SetTexture(MainCubemapId, fromCubemap);
        _runtimeSkyboxMaterial.SetTexture(BlendCubemapId, toCubemap);
        _runtimeSkyboxMaterial.SetFloat(CubemapTransitionId, skyboxProgress);
        _runtimeSkyboxMaterial.SetFloat(FogIntensityId, _skyDatabase.FogIntensity);
        _runtimeSkyboxMaterial.SetFloat(FogPositionId, _skyDatabase.FogPosition);
        _runtimeSkyboxMaterial.SetFloat(FogFillId, _skyDatabase.FogFill);

        // 세그먼트가 바뀌었거나 블렌드 값이 충분히 변했을 때만 GI 갱신 (비용이 높은 연산)
        bool segmentChanged = _lastGIFromSegment != from;
        if (segmentChanged || _lastGIBlendValue < 0f || skyboxProgress - _lastGIBlendValue >= GIUpdateThreshold)
        {
            _lastGIFromSegment = from;
            _lastGIBlendValue = skyboxProgress;
            DynamicGI.UpdateEnvironment();
        }
    }

    // 런타임 머터리얼이 없으면 템플릿 기반으로 생성하고 RenderSettings에 할당한다
    private bool EnsureRuntimeMaterial()
    {
        // 이미 생성된 경우 소유권만 확인
        if (_runtimeSkyboxMaterial != null)
        {
            if (RenderSettings.skybox != _runtimeSkyboxMaterial)
                RenderSettings.skybox = _runtimeSkyboxMaterial;
            return true;
        }

        // 템플릿 머터리얼 결정 (명시 지정 > 현재 RenderSettings)
        Material template = _skyboxTemplate != null ? _skyboxTemplate : RenderSettings.skybox;
        if (template == null || !template.HasProperty(CubemapTransitionId)) return false;

        // 원본 백업 및 템플릿의 기본 Cubemap 캐싱
        _originalSkybox = RenderSettings.skybox;
        _templateMainCubemap = template.GetTexture(MainCubemapId) as Cubemap;
        _templateBlendCubemap = template.GetTexture(BlendCubemapId) as Cubemap;

        // 런타임 전용 머터리얼 복제
        _runtimeSkyboxMaterial = new Material(template)
        {
            name = $"{template.name} (Runtime)",
            hideFlags = HideFlags.DontSave
        };

        _runtimeSkyboxMaterial.EnableKeyword("_ENABLEFOG_ON");
        RenderSettings.skybox = _runtimeSkyboxMaterial;
        return true;
    }

    #endregion
}
