using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class EvolutionTimelineEnergyRiseController : MonoBehaviour
{
    private const string OverlayRootName = "__EvolutionEnergyRiseOverlay";
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int MinYId = Shader.PropertyToID("_MinY");
    private static readonly int MaxYId = Shader.PropertyToID("_MaxY");
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int BandWidthId = Shader.PropertyToID("_BandWidth");
    private static readonly int FillAlphaId = Shader.PropertyToID("_FillAlpha");
    private static readonly int GlowAlphaId = Shader.PropertyToID("_GlowAlpha");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

    [Header("Runtime References")]
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private Transform _beforeModelRoot;
    [SerializeField] private string _focusLayerName = "Evolution_Focus";

    [Header("Material")]
    [SerializeField] private Material _energyRiseMaterial;

    private readonly List<Renderer> _overlayRenderers = new();
    private HelperEvolutionProfileSO _profile;
    private Transform _overlayRoot;
    private bool _isPlaying;
    private float _minY;
    private float _maxY;

    public void Configure(
        HelperEvolutionProfileSO profile,
        PlayableDirector director,
        Transform beforeModelRoot)
    {
        _profile = profile;
        _director = director;
        _beforeModelRoot = beforeModelRoot;
    }

    public void Play()
    {
        _isPlaying = _profile != null && _beforeModelRoot != null;
        if (!_isPlaying) return;

        EnsureMaterial();
        RebuildOverlay();
        ApplyAtTime(0f);
    }

    public void Stop()
    {
        _isPlaying = false;
        SetOverlayVisible(false);
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
        if (_profile == null || _overlayRenderers.Count == 0)
            return;

        float duration = Mathf.Max(0.01f, _profile.BeforeEnergyRiseDuration);
        float endTime = Mathf.Max(duration, _profile.BeforeEnergyRiseEndTime);
        float startTime = endTime - duration;
        float progress = Mathf.InverseLerp(startTime, endTime, time);

        bool visible = time >= startTime && time <= endTime;
        SetOverlayVisible(visible);
        if (!visible) return;

        float easedProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
        foreach (Renderer overlayRenderer in _overlayRenderers)
        {
            if (overlayRenderer == null) continue;
            Material material = overlayRenderer.sharedMaterial;
            if (material == null) continue;

            material.SetColor(ColorId, _profile.BeforeEnergyRiseColor);
            material.SetFloat(MinYId, _minY);
            material.SetFloat(MaxYId, _maxY);
            material.SetFloat(ProgressId, easedProgress);
            material.SetFloat(BandWidthId, Mathf.Max(0.01f, _profile.BeforeEnergyRiseBandWidth));
            material.SetFloat(FillAlphaId, _profile.BeforeEnergyRiseFillAlpha);
            material.SetFloat(GlowAlphaId, _profile.BeforeEnergyRiseGlowAlpha * easedProgress);
            material.SetFloat(OutlineWidthId, Mathf.Max(0f, _profile.BeforeEnergyRiseOutlineWidth));
        }
    }

    private void RebuildOverlay()
    {
        ClearOverlay();
        if (_energyRiseMaterial == null || _beforeModelRoot == null)
            return;

        GameObject root = new GameObject(OverlayRootName);
        root.transform.SetParent(_beforeModelRoot, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        int focusLayer = LayerMask.NameToLayer(_focusLayerName);
        if (focusLayer >= 0)
            root.layer = focusLayer;
        _overlayRoot = root.transform;

        Renderer[] sourceRenderers = _beforeModelRoot.GetComponentsInChildren<Renderer>(true);
        Bounds combinedBounds = new Bounds(_beforeModelRoot.position, Vector3.one);
        bool hasBounds = false;

        foreach (Renderer source in sourceRenderers)
        {
            if (source == null || source.transform.IsChildOf(_overlayRoot)) continue;

            Renderer overlayRenderer = CreateOverlayRenderer(source);
            if (overlayRenderer == null) continue;

            _overlayRenderers.Add(overlayRenderer);
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

        float padding = _profile != null ? Mathf.Max(0f, _profile.BeforeEnergyRiseVerticalPadding) : 0.1f;
        _minY = combinedBounds.min.y - padding;
        _maxY = combinedBounds.max.y + padding;
        SetOverlayVisible(false);
    }

    private Renderer CreateOverlayRenderer(Renderer source)
    {
        GameObject overlay = new GameObject($"{source.gameObject.name}_EnergyRise");
        overlay.transform.SetParent(source.transform, false);
        overlay.transform.localPosition = Vector3.zero;
        overlay.transform.localRotation = Quaternion.identity;
        overlay.transform.localScale = Vector3.one;
        overlay.layer = _overlayRoot != null ? _overlayRoot.gameObject.layer : source.gameObject.layer;

        Material materialInstance = new Material(_energyRiseMaterial);

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

    private void EnsureMaterial()
    {
        if (_energyRiseMaterial != null) return;

        Shader shader = Shader.Find("Farmming/EvolutionEnergyRise");
        if (shader != null)
            _energyRiseMaterial = new Material(shader);
    }

    private void SetOverlayVisible(bool visible)
    {
        if (_overlayRoot != null)
            _overlayRoot.gameObject.SetActive(visible);
    }

    private void ClearOverlay()
    {
        foreach (Renderer overlayRenderer in _overlayRenderers)
        {
            if (overlayRenderer == null || overlayRenderer.sharedMaterial == null) continue;
            Destroy(overlayRenderer.sharedMaterial);
        }
        _overlayRenderers.Clear();

        if (_overlayRoot != null)
        {
            Destroy(_overlayRoot.gameObject);
            _overlayRoot = null;
        }
    }

    private void OnDestroy()
    {
        ClearOverlay();
    }
}
