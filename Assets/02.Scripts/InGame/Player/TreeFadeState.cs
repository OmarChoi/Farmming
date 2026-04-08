using UnityEngine;
using UnityEngine.Rendering;

public abstract class TreeFadeState
{
    public abstract void BeginFade(float transparentAlpha);
    public abstract void Tick(float fadeStep);
    public abstract bool IsFullyOpaque { get; }
    public abstract void Restore();
    public abstract void Dispose();

    public static TreeFadeState Create(Renderer[] renderers)
    {
        if (renderers == null || renderers.Length == 0)
        {
            return EmptyTreeFadeState.Instance;
        }

        if (renderers.Length == 1)
        {
            return new SingleRendererTreeFadeState(renderers[0]);
        }

        return new RendererGroupTreeFadeState(renderers);
    }

    private sealed class EmptyTreeFadeState : TreeFadeState
    {
        public static readonly EmptyTreeFadeState Instance = new();

        public override void BeginFade(float transparentAlpha) { }
        public override void Tick(float fadeStep) { }
        public override bool IsFullyOpaque => true;
        public override void Restore() { }
        public override void Dispose() { }
    }

    private sealed class SingleRendererTreeFadeState : TreeFadeState
    {
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        private static readonly int AlphaClipId = Shader.PropertyToID("_AlphaClip");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly Renderer _renderer;
        private readonly Material[] _originalMaterials;
        private readonly Material[] _transparentMaterials;
        private readonly Color[] _baseColors;
        private readonly Color[] _colorColors;

        private float _currentAlpha = 1f;
        private float _targetAlpha = 1f;
        private bool _isUsingTransparentMaterials;

        public SingleRendererTreeFadeState(Renderer renderer)
        {
            _renderer = renderer;
            _originalMaterials = renderer != null ? renderer.sharedMaterials : System.Array.Empty<Material>();
            _transparentMaterials = new Material[_originalMaterials.Length];
            _baseColors = new Color[_originalMaterials.Length];
            _colorColors = new Color[_originalMaterials.Length];

            for (int i = 0; i < _originalMaterials.Length; i++)
            {
                Material source = _originalMaterials[i];
                if (source == null) continue;

                Material transparent = new Material(source);
                transparent.name = source.name + "_OcclusionRuntime";
                CacheSourceColors(source, i);
                ConfigureTransparentMaterial(transparent);
                _transparentMaterials[i] = transparent;
            }
        }

        public override void BeginFade(float transparentAlpha)
        {
            _targetAlpha = transparentAlpha;
            EnsureTransparentMaterialsApplied();
        }

        public override void Tick(float fadeStep)
        {
            if (_renderer == null) return;

            if (!_isUsingTransparentMaterials && Mathf.Approximately(_targetAlpha, 1f))
            {
                return;
            }

            EnsureTransparentMaterialsApplied();
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, fadeStep);
            ApplyAlpha(_currentAlpha);

            if (Mathf.Approximately(_currentAlpha, 1f) && Mathf.Approximately(_targetAlpha, 1f))
            {
                Restore();
            }
        }

        public override bool IsFullyOpaque => Mathf.Approximately(_currentAlpha, 1f) && Mathf.Approximately(_targetAlpha, 1f);

        public override void Restore()
        {
            if (_renderer == null) return;

            _renderer.sharedMaterials = _originalMaterials;
            _isUsingTransparentMaterials = false;
        }

        public override void Dispose()
        {
            Restore();

            for (int i = 0; i < _transparentMaterials.Length; i++)
            {
                Material material = _transparentMaterials[i];
                if (material == null) continue;
                Object.Destroy(material);
            }
        }

        private void EnsureTransparentMaterialsApplied()
        {
            if (_renderer == null || _isUsingTransparentMaterials) return;

            _renderer.sharedMaterials = _transparentMaterials;
            _isUsingTransparentMaterials = true;
            ApplyAlpha(_currentAlpha);
        }

        private void ApplyAlpha(float alpha)
        {
            for (int i = 0; i < _transparentMaterials.Length; i++)
            {
                Material material = _transparentMaterials[i];
                if (material == null) continue;

                ApplyColorAlpha(material, BaseColorId, _baseColors[i], alpha);
                ApplyColorAlpha(material, ColorId, _colorColors[i], alpha);
            }
        }

        private void CacheSourceColors(Material source, int index)
        {
            _baseColors[index] = source.HasProperty(BaseColorId) ? source.GetColor(BaseColorId) : Color.white;
            _colorColors[index] = source.HasProperty(ColorId) ? source.GetColor(ColorId) : Color.white;
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");

            if (material.HasProperty(SurfaceId)) material.SetFloat(SurfaceId, 1f);
            if (material.HasProperty(BlendId)) material.SetFloat(BlendId, 0f);
            if (material.HasProperty(SrcBlendId)) material.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            if (material.HasProperty(DstBlendId)) material.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty(ZWriteId)) material.SetFloat(ZWriteId, 0f);
            if (material.HasProperty(AlphaClipId)) material.SetFloat(AlphaClipId, 0f);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void ApplyColorAlpha(Material material, int propertyId, Color sourceColor, float alpha)
        {
            if (!material.HasProperty(propertyId)) return;

            Color color = sourceColor;
            color.a = alpha;
            material.SetColor(propertyId, color);
        }
    }

    private sealed class RendererGroupTreeFadeState : TreeFadeState
    {
        private readonly TreeFadeState[] _states;

        public RendererGroupTreeFadeState(Renderer[] renderers)
        {
            _states = new TreeFadeState[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                _states[i] = new SingleRendererTreeFadeState(renderers[i]);
            }
        }

        public override void BeginFade(float transparentAlpha)
        {
            for (int i = 0; i < _states.Length; i++)
            {
                _states[i].BeginFade(transparentAlpha);
            }
        }

        public override void Tick(float fadeStep)
        {
            for (int i = 0; i < _states.Length; i++)
            {
                _states[i].Tick(fadeStep);
            }
        }

        public override bool IsFullyOpaque
        {
            get
            {
                for (int i = 0; i < _states.Length; i++)
                {
                    if (!_states[i].IsFullyOpaque) return false;
                }

                return true;
            }
        }

        public override void Restore()
        {
            for (int i = 0; i < _states.Length; i++)
            {
                _states[i].Restore();
            }
        }

        public override void Dispose()
        {
            for (int i = 0; i < _states.Length; i++)
            {
                _states[i].Dispose();
            }
        }
    }
}
