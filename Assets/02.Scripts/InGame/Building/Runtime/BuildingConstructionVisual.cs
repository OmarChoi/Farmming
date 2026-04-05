using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class BuildingConstructionVisual
{
    private static readonly int RevealEnabledId = Shader.PropertyToID("_ConstructionRevealEnabled");
    private static readonly int RevealYId = Shader.PropertyToID("_ConstructionRevealY");
    private static readonly int RevealFeatherId = Shader.PropertyToID("_ConstructionRevealFeather");
    private static readonly int RevealInvertId = Shader.PropertyToID("_ConstructionRevealInvert");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    private const float MinimumHeight = 0.01f;
    private const float DefaultFeather = 0.08f;
    private const string GhostNameSuffix = "_ConstructionGhost";

    private readonly List<RendererRecord> _records = new List<RendererRecord>();
    private readonly List<Material> _runtimeMaterials = new List<Material>();

    private float _minY;
    private float _maxY;
    private float _feather;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public void Initialize(
        GameObject root,
        Shader revealLitShader,
        Shader ghostRevealShader,
        Material ghostBaseMaterial,
        float feather = DefaultFeather)
    {
        Dispose();

        if (root == null || revealLitShader == null || ghostRevealShader == null) return;

        _feather = Mathf.Max(0.001f, feather);

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
        Bounds bounds = default;
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            if (renderer is ParticleSystemRenderer || renderer.name.EndsWith(GhostNameSuffix)) continue;

            Material[] originalMaterials = renderer.sharedMaterials;
            if (originalMaterials == null || originalMaterials.Length == 0) continue;

            Material[] revealMaterials = CreateRevealMaterials(originalMaterials, revealLitShader);
            Renderer overlayRenderer = CreateOverlayRenderer(renderer, originalMaterials, ghostRevealShader, ghostBaseMaterial);
            if (overlayRenderer == null)
            {
                DestroyMaterials(revealMaterials);
                RemoveRuntimeMaterials(revealMaterials);
                continue;
            }

            renderer.sharedMaterials = revealMaterials;
            _records.Add(new RendererRecord(renderer, originalMaterials, revealMaterials, overlayRenderer));

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (_records.Count == 0) return;

        _minY = hasBounds ? bounds.min.y : root.transform.position.y;
        _maxY = hasBounds ? Mathf.Max(bounds.max.y, _minY + MinimumHeight) : _minY + 1f;
        _isInitialized = true;
    }

    public void ApplyProgress(float progress, bool isComplete)
    {
        if (!_isInitialized) return;

        if (isComplete)
        {
            ReleaseRuntimeState();
            return;
        }

        float revealY = Mathf.Lerp(_minY - _feather, _maxY + _feather, Mathf.Clamp01(progress));

        foreach (RendererRecord record in _records)
        {
            if (record.SourceRenderer == null) continue;

            SetRevealProperties(record.RevealMaterials, revealY, invert: false);

            if (record.OverlayRenderer != null)
            {
                record.OverlayRenderer.enabled = record.SourceRenderer.enabled;
                SetRevealProperties(record.OverlayRenderer.sharedMaterials, revealY, invert: true);
            }
        }
    }

    public void Dispose()
    {
        ReleaseRuntimeState();
    }

    private Material[] CreateRevealMaterials(Material[] originals, Shader revealLitShader)
    {
        var revealMaterials = new Material[originals.Length];
        for (int i = 0; i < originals.Length; i++)
        {
            Material runtimeMaterial = new Material(revealLitShader);
            CopyLitProperties(originals[i], runtimeMaterial);
            revealMaterials[i] = runtimeMaterial;
            _runtimeMaterials.Add(runtimeMaterial);
        }

        return revealMaterials;
    }

    private Renderer CreateOverlayRenderer(
        Renderer sourceRenderer,
        Material[] originalMaterials,
        Shader ghostRevealShader,
        Material ghostBaseMaterial)
    {
        Transform sourceTransform = sourceRenderer.transform;
        var overlayObject = new GameObject(sourceRenderer.name + GhostNameSuffix);
        overlayObject.layer = sourceRenderer.gameObject.layer;
        Transform overlayTransform = overlayObject.transform;
        overlayTransform.SetParent(sourceTransform, false);
        overlayTransform.localPosition = Vector3.zero;
        overlayTransform.localRotation = Quaternion.identity;
        overlayTransform.localScale = Vector3.one;

        Renderer overlayRenderer = null;

        if (sourceRenderer is MeshRenderer meshRenderer)
        {
            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                Object.Destroy(overlayObject);
                return null;
            }

            MeshFilter overlayFilter = overlayObject.AddComponent<MeshFilter>();
            overlayFilter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer overlayMeshRenderer = overlayObject.AddComponent<MeshRenderer>();
            CopyRendererSettings(meshRenderer, overlayMeshRenderer);
            overlayRenderer = overlayMeshRenderer;
        }
        else if (sourceRenderer is SkinnedMeshRenderer skinnedRenderer)
        {
            SkinnedMeshRenderer overlaySkinnedRenderer = overlayObject.AddComponent<SkinnedMeshRenderer>();
            CopyRendererSettings(skinnedRenderer, overlaySkinnedRenderer);
            overlaySkinnedRenderer.sharedMesh = skinnedRenderer.sharedMesh;
            overlaySkinnedRenderer.bones = skinnedRenderer.bones;
            overlaySkinnedRenderer.rootBone = skinnedRenderer.rootBone;
            overlaySkinnedRenderer.localBounds = skinnedRenderer.localBounds;
            overlaySkinnedRenderer.updateWhenOffscreen = skinnedRenderer.updateWhenOffscreen;
            overlayRenderer = overlaySkinnedRenderer;
        }

        if (overlayRenderer == null)
        {
            Object.Destroy(overlayObject);
            return null;
        }

        overlayRenderer.sharedMaterials = CreateGhostMaterials(originalMaterials, ghostRevealShader, ghostBaseMaterial);
        overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
        overlayRenderer.receiveShadows = false;
        overlayRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        overlayRenderer.enabled = sourceRenderer.enabled;
        return overlayRenderer;
    }

    private Material[] CreateGhostMaterials(Material[] originals, Shader ghostRevealShader, Material ghostBaseMaterial)
    {
        var ghostMaterials = new Material[originals.Length];

        for (int i = 0; i < originals.Length; i++)
        {
            Material runtimeMaterial = new Material(ghostRevealShader);
            if (ghostBaseMaterial != null)
            {
                runtimeMaterial.CopyPropertiesFromMaterial(ghostBaseMaterial);
            }
            else
            {
                runtimeMaterial.SetColor(BaseColorId, new Color(1f, 1f, 1f, 0.45f));
            }

            if (originals[i] != null
                && runtimeMaterial.HasProperty(BaseMapId)
                && originals[i].HasProperty(BaseMapId)
                && runtimeMaterial.GetTexture(BaseMapId) == null)
            {
                runtimeMaterial.SetTexture(BaseMapId, originals[i].GetTexture(BaseMapId));
            }

            ghostMaterials[i] = runtimeMaterial;
            _runtimeMaterials.Add(runtimeMaterial);
        }

        return ghostMaterials;
    }

    private void SetRevealProperties(Material[] materials, float revealY, bool invert)
    {
        if (materials == null) return;

        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null) continue;

            material.SetFloat(RevealEnabledId, 1f);
            material.SetFloat(RevealYId, revealY);
            material.SetFloat(RevealFeatherId, _feather);
            material.SetFloat(RevealInvertId, invert ? 1f : 0f);
        }
    }

    private void RestoreOriginalMaterials()
    {
        foreach (RendererRecord record in _records)
        {
            if (record.SourceRenderer != null)
            {
                record.SourceRenderer.sharedMaterials = record.OriginalMaterials;
            }
        }
    }

    private void ReleaseRuntimeState()
    {
        RestoreOriginalMaterials();

        foreach (RendererRecord record in _records)
        {
            if (record.OverlayRenderer != null)
            {
                Object.Destroy(record.OverlayRenderer.gameObject);
            }
        }

        DestroyMaterials(_runtimeMaterials);
        _records.Clear();
        _runtimeMaterials.Clear();
        _isInitialized = false;
    }

    private void CopyLitProperties(Material source, Material destination)
    {
        if (destination == null) return;

        if (source != null)
        {
            destination.CopyPropertiesFromMaterial(source);
            destination.renderQueue = source.renderQueue;
            destination.enableInstancing = source.enableInstancing;
        }

        if (destination.HasProperty(RevealEnabledId))
        {
            destination.SetFloat(RevealEnabledId, 1f);
            destination.SetFloat(RevealFeatherId, _feather);
        }

        bool hasNormalMap = source != null && source.HasProperty("_BumpMap") && source.GetTexture("_BumpMap") != null;
        SetKeyword(destination, "_NORMALMAP", hasNormalMap);

        bool hasEmission = false;
        if (source != null && source.HasProperty("_EmissionColor"))
        {
            Color emissionColor = source.GetColor("_EmissionColor");
            hasEmission = emissionColor.maxColorComponent > 0.0001f;
        }
        if (!hasEmission && source != null && source.HasProperty("_EmissionMap"))
        {
            hasEmission = source.GetTexture("_EmissionMap") != null;
        }
        SetKeyword(destination, "_EMISSION", hasEmission);

        bool alphaClip = source != null && source.HasProperty("_AlphaClip") && source.GetFloat("_AlphaClip") > 0.5f;
        SetKeyword(destination, "_ALPHATEST_ON", alphaClip);
    }

    private static void CopyRendererSettings(Renderer source, Renderer destination)
    {
        destination.lightProbeUsage = source.lightProbeUsage;
        destination.reflectionProbeUsage = source.reflectionProbeUsage;
        destination.renderingLayerMask = source.renderingLayerMask;
        destination.probeAnchor = source.probeAnchor;
        destination.sortingLayerID = source.sortingLayerID;
        destination.sortingOrder = source.sortingOrder;
        destination.enabled = source.enabled;
    }

    private static void SetKeyword(Material material, string keyword, bool enabled)
    {
        if (material == null) return;

        if (enabled)
        {
            material.EnableKeyword(keyword);
        }
        else
        {
            material.DisableKeyword(keyword);
        }
    }

    private void RemoveRuntimeMaterials(Material[] materials)
    {
        if (materials == null) return;

        for (int i = 0; i < materials.Length; i++)
        {
            _runtimeMaterials.Remove(materials[i]);
        }
    }

    private static void DestroyMaterials(IList<Material> materials)
    {
        if (materials == null) return;
        foreach (Material mt in materials)
        {
            if (mt != null)
            {
                Object.Destroy(mt);
            }
        }
    }

    private readonly struct RendererRecord
    {
        public readonly Renderer SourceRenderer;
        public readonly Material[] OriginalMaterials;
        public readonly Material[] RevealMaterials;
        public readonly Renderer OverlayRenderer;

        public RendererRecord(
            Renderer sourceRenderer,
            Material[] originalMaterials,
            Material[] revealMaterials,
            Renderer overlayRenderer)
        {
            SourceRenderer = sourceRenderer;
            OriginalMaterials = originalMaterials;
            RevealMaterials = revealMaterials;
            OverlayRenderer = overlayRenderer;
        }
    }
}
