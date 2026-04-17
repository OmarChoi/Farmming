using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class EvolutionTimelineEnergyRiseController : MonoBehaviour
{
    private const string OverlayRootName = "__EvolutionEnergyRiseOverlay";
    private const float RevealModeRise = 0f;
    private const float RevealModeClearFromBottom = 1f;

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

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int MinYId = Shader.PropertyToID("_MinY");
    private static readonly int MaxYId = Shader.PropertyToID("_MaxY");
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int RevealModeId = Shader.PropertyToID("_RevealMode");
    private static readonly int BandWidthId = Shader.PropertyToID("_BandWidth");
    private static readonly int FillAlphaId = Shader.PropertyToID("_FillAlpha");
    private static readonly int GlowAlphaId = Shader.PropertyToID("_GlowAlpha");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int RainbowStrengthId = Shader.PropertyToID("_RainbowStrength");
    private static readonly int RainbowSpeedId = Shader.PropertyToID("_RainbowSpeed");
    private static readonly int SurfaceOffsetId = Shader.PropertyToID("_SurfaceOffset");
    private static readonly int EffectStrengthId = Shader.PropertyToID("_EffectStrength");
    private static readonly int GalaxyTextureId = Shader.PropertyToID("_GalaxyTexture");
    private static readonly int GalaxyTintId = Shader.PropertyToID("_GalaxyTint");
    private static readonly int GalaxyEmissionIntensityId = Shader.PropertyToID("_GalaxyEmissionIntensity");
    private static readonly int GalaxyBaseColorIntensityId = Shader.PropertyToID("_GalaxyBaseColorIntensity");
    private static readonly int StarsTextureId = Shader.PropertyToID("_StarsTexture");
    private static readonly int StarsTileId = Shader.PropertyToID("_StarsTile");
    private static readonly int StarsTileOverallId = Shader.PropertyToID("_StarsTileOverall");
    private static readonly int StarsSpeedId = Shader.PropertyToID("_StarsSpeed");
    private static readonly int StarsColor01Id = Shader.PropertyToID("_StarsColor01");
    private static readonly int StarsColor02Id = Shader.PropertyToID("_StarsColor02");
    private static readonly int StarsEmissionIntensityId = Shader.PropertyToID("_StarsEmissionIntensity");
    private static readonly int StarsNoiseTextureId = Shader.PropertyToID("_StarsNoiseTexture");
    private static readonly int StarsNoiseTileId = Shader.PropertyToID("_StarsNoiseTile");
    private static readonly int StarsNoiseTileOverallId = Shader.PropertyToID("_StarsNoiseTileOverall");
    private static readonly int StarsNoiseSpeedId = Shader.PropertyToID("_StarsNoiseSpeed");
    private static readonly int FresnelColorId = Shader.PropertyToID("_FresnelColor");
    private static readonly int FresnelEmissionIntensityId = Shader.PropertyToID("_FresnelEmissionIntensity");
    private static readonly int FresnelBiasId = Shader.PropertyToID("_FresnelBias");
    private static readonly int FresnelScaleId = Shader.PropertyToID("_FresnelScale");
    private static readonly int FresnelPowerId = Shader.PropertyToID("_FresnelPower");

    [Header("Runtime References")]
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private Transform _beforeModelRoot;
    [SerializeField] private Transform _afterModelRoot;
    [SerializeField] private string _focusLayerName = "Evolution_Focus";

    [Header("Material")]
    [SerializeField] private Material _energyRiseMaterial;

    private readonly OverlayState _beforeOverlay = new();
    private readonly OverlayState _afterOverlay = new();
    private HelperEvolutionProfileSO _profile;
    private bool _isPlaying;

    private sealed class OverlayState
    {
        public readonly List<Renderer> Renderers = new();
        public Transform Root;
        public float MinY;
        public float MaxY;
    }

    public void Configure(
        HelperEvolutionProfileSO profile,
        PlayableDirector director,
        Transform beforeModelRoot,
        Transform afterModelRoot)
    {
        _profile = profile;
        _director = director;
        _beforeModelRoot = beforeModelRoot;
        _afterModelRoot = afterModelRoot;
    }

    public void Play()
    {
        _isPlaying = _profile != null && (_beforeModelRoot != null || _afterModelRoot != null);
        if (!_isPlaying) return;

        EnsureMaterial();
        RebuildOverlay(_beforeOverlay, _beforeModelRoot, GetBeforeSourceMaterial(), _profile.BeforeEnergyRiseVerticalPadding);
        RebuildOverlay(_afterOverlay, _afterModelRoot, GetAfterSourceMaterial(), _profile.AfterEnergyClearVerticalPadding);
        ApplyAtTime(0f);
    }

    public void Stop()
    {
        _isPlaying = false;
        SetOverlayVisible(_beforeOverlay, false);
        SetOverlayVisible(_afterOverlay, false);
    }

    private void LateUpdate()
    {
        if (!_isPlaying) return;

        float time = _director != null
            ? (float)_director.time
            : Time.time;

        ApplyAtTime(time);
    }

    private void ApplyAtTime(float time)
    {
        if (_profile == null)
            return;

        ApplyBeforeOverlayAtTime(time);
        ApplyAfterOverlayAtTime(time);
    }

    private void ApplyBeforeOverlayAtTime(float time)
    {
        if (_beforeOverlay.Renderers.Count == 0)
            return;

        float duration = Mathf.Max(0.01f, _profile.BeforeEnergyRiseDuration);
        float startTime = Mathf.Max(0f, _profile.BeforeEnergyRiseStartDelay);
        float endTime = startTime + duration;
        bool visible = time >= startTime && time <= endTime;
        SetOverlayVisible(_beforeOverlay, visible);
        if (!visible) return;

        float progress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(startTime, endTime, time));
        ApplyOverlayMaterials(
            _beforeOverlay,
            progress,
            RevealModeRise,
            _profile.BeforeEnergyRiseBandWidth,
            _profile.BeforeEnergyRiseFillAlpha,
            _profile.BeforeEnergyRiseGlowAlpha * progress,
            _profile.BeforeEnergyRiseEffectStrength,
            _profile.BeforeEnergyRiseOutlineWidth,
            _profile.BeforeEnergyRiseSurfaceOffset);
    }

    private void ApplyAfterOverlayAtTime(float time)
    {
        if (_afterOverlay.Renderers.Count == 0)
            return;

        GetAfterClearTimes(out float clearStart, out float clearEnd);
        float visibleStart = Mathf.Max(0f, _profile.ModelSwapTime);
        bool visible = time >= visibleStart && time <= clearEnd;
        SetOverlayVisible(_afterOverlay, visible);
        if (!visible) return;

        float progress = time < clearStart
            ? 0f
            : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(clearStart, clearEnd, time));

        ApplyOverlayMaterials(
            _afterOverlay,
            progress,
            RevealModeClearFromBottom,
            _profile.AfterEnergyClearBandWidth,
            _profile.AfterEnergyClearFillAlpha,
            _profile.AfterEnergyClearGlowAlpha,
            _profile.AfterEnergyClearEffectStrength,
            _profile.AfterEnergyClearOutlineWidth,
            _profile.AfterEnergyClearSurfaceOffset);
    }

    private void ApplyOverlayMaterials(
        OverlayState overlay,
        float progress,
        float revealMode,
        float bandWidth,
        float fillAlpha,
        float glowAlpha,
        float effectStrength,
        float outlineWidth,
        float surfaceOffset)
    {
        foreach (Renderer overlayRenderer in overlay.Renderers)
        {
            if (overlayRenderer == null) continue;
            Material material = overlayRenderer.sharedMaterial;
            if (material == null) continue;

            material.SetColor(ColorId, _profile.BeforeEnergyRiseColor);
            material.SetFloat(MinYId, overlay.MinY);
            material.SetFloat(MaxYId, overlay.MaxY);
            material.SetFloat(ProgressId, Mathf.Clamp01(progress));
            material.SetFloat(RevealModeId, revealMode);
            material.SetFloat(BandWidthId, Mathf.Max(0.01f, bandWidth));
            material.SetFloat(FillAlphaId, Mathf.Clamp01(fillAlpha));
            material.SetFloat(GlowAlphaId, Mathf.Max(0f, glowAlpha));
            material.SetFloat(OutlineWidthId, Mathf.Max(0f, outlineWidth));
            material.SetFloat(RainbowStrengthId, Mathf.Clamp01(_profile.BeforeEnergyRiseRainbowStrength));
            material.SetFloat(RainbowSpeedId, Mathf.Max(0f, _profile.BeforeEnergyRiseRainbowSpeed));
            material.SetFloat(SurfaceOffsetId, Mathf.Max(0f, surfaceOffset));
            material.SetFloat(EffectStrengthId, Mathf.Max(0f, effectStrength));
        }
    }

    private void GetAfterClearTimes(out float clearStart, out float clearEnd)
    {
        float introEnd = Mathf.Max(0f, _profile.IntroDuration);
        float zoomEnd = introEnd + Mathf.Max(0.01f, _profile.ImpactZoomDuration);
        float pullbackEnd = zoomEnd + Mathf.Max(0.01f, _profile.AfterPullbackDuration);
        float timelineEnd = GetTimelineEndTime();
        float showcaseDuration = Mathf.Max(0.01f, _profile.FinalShowcaseDuration);
        float showcaseStart = Mathf.Max(pullbackEnd + 0.01f, timelineEnd - showcaseDuration);
        float scanEnd = Mathf.Min(
            pullbackEnd + Mathf.Max(0.01f, _profile.AfterScanDuration),
            showcaseStart);
        if (scanEnd <= pullbackEnd)
            scanEnd = pullbackEnd + 0.01f;

        clearStart = Mathf.Max(_profile.ModelSwapTime, pullbackEnd + _profile.AfterEnergyClearStartOffset);
        clearEnd = scanEnd + _profile.AfterEnergyClearEndOffset;
        if (clearEnd <= clearStart)
            clearEnd = clearStart + 0.01f;
    }

    private float GetTimelineEndTime()
    {
        if (_director != null &&
            _director.playableAsset != null &&
            !double.IsNaN(_director.duration) &&
            !double.IsInfinity(_director.duration) &&
            _director.duration > 0d)
        {
            return (float)_director.duration;
        }

        return _profile.IntroDuration
            + _profile.ImpactZoomDuration
            + _profile.AfterPullbackDuration
            + _profile.AfterScanDuration
            + _profile.FinalShowcaseDuration;
    }

    private void RebuildOverlay(OverlayState overlay, Transform modelRoot, Material sourceMaterial, float verticalPadding)
    {
        ClearOverlay(overlay);
        if (_energyRiseMaterial == null || modelRoot == null)
            return;

        GameObject root = new GameObject(OverlayRootName);
        root.transform.SetParent(modelRoot, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        root.SetActive(false);

        int focusLayer = LayerMask.NameToLayer(_focusLayerName);
        if (focusLayer >= 0)
            root.layer = focusLayer;
        overlay.Root = root.transform;

        Renderer[] sourceRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        Bounds combinedBounds = new Bounds(modelRoot.position, Vector3.one);
        bool hasBounds = false;

        foreach (Renderer source in sourceRenderers)
        {
            if (source == null || source.transform.IsChildOf(overlay.Root)) continue;
            if (IsCutsceneHiddenTransform(source.transform)) continue;

            Renderer overlayRenderer = CreateOverlayRenderer(overlay, source, sourceMaterial);
            if (overlayRenderer == null) continue;

            overlay.Renderers.Add(overlayRenderer);
            if (!hasBounds)
            {
                combinedBounds = source.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(source.bounds);
            }
        }

        float padding = Mathf.Max(0f, verticalPadding);
        overlay.MinY = combinedBounds.min.y - padding;
        overlay.MaxY = combinedBounds.max.y + padding;
        ApplyInitialHiddenState(overlay);
        SetOverlayVisible(overlay, false);
    }

    private Renderer CreateOverlayRenderer(OverlayState overlayState, Renderer source, Material sourceMaterial)
    {
        if (IsCutsceneHiddenTransform(source.transform))
            return null;

        GameObject overlay = new GameObject($"{source.gameObject.name}_EnergyRise");
        overlay.transform.SetParent(source.transform, false);
        overlay.transform.localPosition = Vector3.zero;
        overlay.transform.localRotation = Quaternion.identity;
        overlay.transform.localScale = Vector3.one;
        overlay.layer = overlayState.Root != null ? overlayState.Root.gameObject.layer : source.gameObject.layer;

        Material materialInstance = new Material(_energyRiseMaterial);
        CopyGalaxyMaterialProperties(materialInstance, sourceMaterial);
        InitializeHiddenMaterial(materialInstance);

        if (source is SkinnedMeshRenderer sourceSkinned)
        {
            SkinnedMeshRenderer overlaySkinned = overlay.AddComponent<SkinnedMeshRenderer>();
            overlaySkinned.sharedMesh = sourceSkinned.sharedMesh;
            overlaySkinned.rootBone = sourceSkinned.rootBone;
            overlaySkinned.bones = sourceSkinned.bones;
            overlaySkinned.localBounds = sourceSkinned.localBounds;
            overlaySkinned.updateWhenOffscreen = true;
            overlaySkinned.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            overlaySkinned.receiveShadows = false;
            overlaySkinned.sharedMaterial = materialInstance;
            return overlaySkinned;
        }

        MeshFilter sourceMeshFilter = source.GetComponent<MeshFilter>();
        if (source is MeshRenderer && sourceMeshFilter != null && sourceMeshFilter.sharedMesh != null)
        {
            MeshFilter overlayMeshFilter = overlay.AddComponent<MeshFilter>();
            overlayMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;
            MeshRenderer overlayMeshRenderer = overlay.AddComponent<MeshRenderer>();
            overlayMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            overlayMeshRenderer.receiveShadows = false;
            overlayMeshRenderer.sharedMaterial = materialInstance;
            return overlayMeshRenderer;
        }

        Destroy(overlay);
        Destroy(materialInstance);
        return null;
    }

    private Material GetBeforeSourceMaterial()
    {
        return _profile != null ? _profile.BeforeEnergyRiseSourceMaterial : null;
    }

    private Material GetAfterSourceMaterial()
    {
        if (_profile == null)
            return null;

        return _profile.AfterEnergyClearSourceMaterial != null
            ? _profile.AfterEnergyClearSourceMaterial
            : _profile.BeforeEnergyRiseSourceMaterial;
    }

    private static bool IsCutsceneHiddenTransform(Transform transform)
    {
        while (transform != null)
        {
            string lowerName = transform.name.ToLowerInvariant();
            foreach (string token in CutsceneHiddenTransformNameTokens)
            {
                if (lowerName.Contains(token))
                    return true;
            }

            transform = transform.parent;
        }

        return false;
    }

    private void ApplyInitialHiddenState(OverlayState overlay)
    {
        foreach (Renderer overlayRenderer in overlay.Renderers)
        {
            if (overlayRenderer == null || overlayRenderer.sharedMaterial == null)
                continue;

            InitializeHiddenMaterial(overlayRenderer.sharedMaterial);
        }
    }

    private static void InitializeHiddenMaterial(Material material)
    {
        if (material == null)
            return;

        SetFloatIfPresent(material, ProgressId, 0f);
        SetFloatIfPresent(material, FillAlphaId, 0f);
        SetFloatIfPresent(material, GlowAlphaId, 0f);
        SetFloatIfPresent(material, EffectStrengthId, 0f);
    }

    private static void CopyGalaxyMaterialProperties(Material target, Material source)
    {
        if (target == null || source == null)
            return;

        CopyTexture(source, target, GalaxyTextureId);
        CopyColor(source, target, GalaxyTintId);
        CopyFloat(source, target, GalaxyEmissionIntensityId);
        CopyFloat(source, target, GalaxyBaseColorIntensityId);

        CopyTexture(source, target, StarsTextureId);
        CopyVector(source, target, StarsTileId);
        CopyFloat(source, target, StarsTileOverallId);
        CopyVector(source, target, StarsSpeedId);
        CopyColor(source, target, StarsColor01Id);
        CopyColor(source, target, StarsColor02Id);
        CopyFloat(source, target, StarsEmissionIntensityId);

        CopyTexture(source, target, StarsNoiseTextureId);
        CopyVector(source, target, StarsNoiseTileId);
        CopyFloat(source, target, StarsNoiseTileOverallId);
        CopyVector(source, target, StarsNoiseSpeedId);

        CopyColor(source, target, FresnelColorId);
        CopyFloat(source, target, FresnelEmissionIntensityId);
        CopyFloat(source, target, FresnelBiasId);
        CopyFloat(source, target, FresnelScaleId);
        CopyFloat(source, target, FresnelPowerId);
    }

    private static void CopyTexture(Material source, Material target, int propertyId)
    {
        if (source.HasProperty(propertyId) && target.HasProperty(propertyId))
            target.SetTexture(propertyId, source.GetTexture(propertyId));
    }

    private static void CopyColor(Material source, Material target, int propertyId)
    {
        if (source.HasProperty(propertyId) && target.HasProperty(propertyId))
            target.SetColor(propertyId, source.GetColor(propertyId));
    }

    private static void CopyFloat(Material source, Material target, int propertyId)
    {
        if (source.HasProperty(propertyId) && target.HasProperty(propertyId))
            target.SetFloat(propertyId, source.GetFloat(propertyId));
    }

    private static void CopyVector(Material source, Material target, int propertyId)
    {
        if (source.HasProperty(propertyId) && target.HasProperty(propertyId))
            target.SetVector(propertyId, source.GetVector(propertyId));
    }

    private static void SetFloatIfPresent(Material target, int propertyId, float value)
    {
        if (target.HasProperty(propertyId))
            target.SetFloat(propertyId, value);
    }

    private void EnsureMaterial()
    {
        if (_energyRiseMaterial != null) return;

        Shader shader = Shader.Find("Farmming/EvolutionEnergyRise");
        if (shader != null)
            _energyRiseMaterial = new Material(shader);
    }

    private static void SetOverlayVisible(OverlayState overlay, bool visible)
    {
        if (overlay.Root != null)
            overlay.Root.gameObject.SetActive(visible);
    }

    private void ClearOverlay(OverlayState overlay)
    {
        foreach (Renderer overlayRenderer in overlay.Renderers)
        {
            if (overlayRenderer == null || overlayRenderer.sharedMaterial == null) continue;
            Destroy(overlayRenderer.sharedMaterial);
        }
        overlay.Renderers.Clear();

        if (overlay.Root != null)
        {
            Destroy(overlay.Root.gameObject);
            overlay.Root = null;
        }
    }

    private void ClearAllOverlays()
    {
        ClearOverlay(_beforeOverlay);
        ClearOverlay(_afterOverlay);
    }

    private void OnDestroy()
    {
        ClearAllOverlays();
    }
}
