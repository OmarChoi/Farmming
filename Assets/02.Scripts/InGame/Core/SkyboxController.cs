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

    private Material _originalSkybox;
    private Material _runtimeSkyboxMaterial;
    private Cubemap _templateMainCubemap;
    private Cubemap _templateBlendCubemap;

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
    private void UpdateSkybox(GameTime time)
    {
        if (!EnsureRuntimeMaterial() || _timeSettings == null || _skyDatabase == null) return;
        if (!TryGetActiveSegment(time, out SkyKeyframeSO from, out SkyKeyframeSO to, out float segmentProgress)) return;
        if (from == null || to == null) return;

        // 커브를 적용하여 비선형 전환 속도를 계산
        float skyboxProgress = EvaluateCurve(from.BlendCurve, segmentProgress);

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

        DynamicGI.UpdateEnvironment();
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

    #region Time Segment

    // 현재 시간이 속한 시간대 세그먼트와 해당 구간 내 선형 진행도를 반환한다
    // [0~dayStart] Night→Dawn / [dayStart~sunrise] Dawn→Day / [sunrise~sunset] Day→Dusk / [sunset~dayEnd] Dusk→Night
    private bool TryGetActiveSegment(GameTime time, out SkyKeyframeSO from, out SkyKeyframeSO to, out float progress)
    {
        from = null;
        to = null;
        progress = 0f;

        // TimeSettings의 시간 경계를 분 단위로 변환
        int dayStart = _timeSettings.DayStartTime.TotalMinutes;
        int sunrise = _timeSettings.SunriseTime.TotalMinutes;
        int sunset = _timeSettings.SunsetTime.TotalMinutes;
        int dayEnd = _timeSettings.DayEndTime.TotalMinutes;
        int current = time.TotalMinutes;

        // 시간 경계 순서가 올바르지 않으면 갱신하지 않음
        if (dayStart > sunrise || sunrise > sunset || sunset > dayEnd || dayEnd > GameTime.MinutesPerDay) return false;

        // 현재 시간이 속한 세그먼트를 순차 탐색
        if (TryGetLinearProgress(current, 0, dayStart, out progress))
        {
            from = _skyDatabase.Get(ESkyType.Night);
            to = _skyDatabase.Get(ESkyType.Dawn);
            return true;
        }

        if (TryGetLinearProgress(current, dayStart, sunrise, out progress))
        {
            from = _skyDatabase.Get(ESkyType.Dawn);
            to = _skyDatabase.Get(ESkyType.Day);
            return true;
        }

        if (TryGetLinearProgress(current, sunrise, sunset, out progress))
        {
            from = _skyDatabase.Get(ESkyType.Day);
            to = _skyDatabase.Get(ESkyType.Dusk);
            return true;
        }

        if (TryGetLinearProgress(current, sunset, dayEnd, out progress))
        {
            from = _skyDatabase.Get(ESkyType.Dusk);
            to = _skyDatabase.Get(ESkyType.Night);
            return true;
        }

        // dayEnd 이후는 밤
        from = _skyDatabase.Get(ESkyType.Night);
        to = _skyDatabase.Get(ESkyType.Night);
        return true;
    }

    // current가 [start, end) 범위에 있으면 구간 내 선형 진행도(0~1)를 계산한다
    private static bool TryGetLinearProgress(int current, int start, int end, out float progress)
    {
        progress = 0f;
        if (end <= start || current < start || current >= end) return false;
        progress = (current - start) / (float)(end - start);
        return true;
    }

    #endregion

    #region Utility

    // AnimationCurve를 적용하되, 키가 없으면 선형 값을 그대로 사용한다
    private static float EvaluateCurve(AnimationCurve curve, float value) 
        => Mathf.Clamp01(curve is { length: > 0 } ? curve.Evaluate(value) : value);

    #endregion
}
