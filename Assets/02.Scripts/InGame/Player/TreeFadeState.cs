using System.Collections.Generic;
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
        private readonly Renderer _renderer;
        private readonly Material[] _originalMaterials;
        private readonly Material[] _pooledMaterials;
        private readonly Color[] _baseColors;
        private readonly Color[] _colorColors;

        private float _currentAlpha = 1f;
        private float _targetAlpha = 1f;
        private bool _isUsingPooledMaterials;

        public SingleRendererTreeFadeState(Renderer renderer)
        {
            _renderer = renderer;
            _originalMaterials = renderer != null ? renderer.sharedMaterials : System.Array.Empty<Material>();
            _pooledMaterials = new Material[_originalMaterials.Length];
            _baseColors = new Color[_originalMaterials.Length];
            _colorColors = new Color[_originalMaterials.Length];

            for (int i = 0; i < _originalMaterials.Length; i++)
            {
                Material source = _originalMaterials[i];
                if (source == null) continue;

                _baseColors[i] = MaterialAlphaPool.GetMaterialColor(source, MaterialAlphaPool.BaseColorId);
                _colorColors[i] = MaterialAlphaPool.GetMaterialColor(source, MaterialAlphaPool.ColorId);
                _pooledMaterials[i] = MaterialAlphaPool.Rent(source);
            }
        }

        public override void BeginFade(float transparentAlpha)
        {
            _targetAlpha = transparentAlpha;
            EnsurePooledMaterialsApplied();
        }

        public override void Tick(float fadeStep)
        {
            if (_renderer == null) return;

            if (!_isUsingPooledMaterials && Mathf.Approximately(_targetAlpha, 1f))
            {
                return;
            }

            EnsurePooledMaterialsApplied();
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
            _isUsingPooledMaterials = false;
        }

        public override void Dispose()
        {
            Restore();

            for (int i = 0; i < _pooledMaterials.Length; i++)
            {
                Material pooledMaterial = _pooledMaterials[i];
                if (pooledMaterial == null) continue;

                MaterialAlphaPool.Return(_originalMaterials[i], pooledMaterial);
                _pooledMaterials[i] = null;
            }
        }

        private void EnsurePooledMaterialsApplied()
        {
            if (_renderer == null || _isUsingPooledMaterials) return;

            _renderer.sharedMaterials = _pooledMaterials;
            _isUsingPooledMaterials = true;
            ApplyAlpha(_currentAlpha);
        }

        private void ApplyAlpha(float alpha)
        {
            for (int i = 0; i < _pooledMaterials.Length; i++)
            {
                Material pooledMaterial = _pooledMaterials[i];
                if (pooledMaterial == null) continue;

                MaterialAlphaPool.ApplyAlpha(pooledMaterial, _baseColors[i], _colorColors[i], alpha);
            }
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

    private static class MaterialAlphaPool
    {
        public static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        public static readonly int BlendId = Shader.PropertyToID("_Blend");
        public static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        public static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        public static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        public static readonly int AlphaClipId = Shader.PropertyToID("_AlphaClip");
        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public static readonly int ColorId = Shader.PropertyToID("_Color");

        private static readonly Dictionary<Material, Stack<Material>> Pool = new();

        public static Material Rent(Material source)
        {
            if (source == null) return null;

            if (Pool.TryGetValue(source, out Stack<Material> stack) && stack.Count > 0)
            {
                return stack.Pop();
            }

            Material material = new Material(source);
            material.name = source.name + "_OcclusionPooled";
            ConfigureTransparent(material);
            return material;
        }

        public static void Return(Material source, Material instance)
        {
            if (source == null || instance == null) return;

            ApplyAlpha(instance, GetMaterialColor(source, BaseColorId), GetMaterialColor(source, ColorId), 1f);

            if (!Pool.TryGetValue(source, out Stack<Material> stack))
            {
                stack = new Stack<Material>();
                Pool.Add(source, stack);
            }

            stack.Push(instance);
        }

        public static void ApplyAlpha(Material material, Color baseColor, Color color, float alpha)
        {
            if (material == null) return;

            if (material.HasProperty(BaseColorId))
            {
                Color nextBaseColor = baseColor;
                nextBaseColor.a = alpha;
                material.SetColor(BaseColorId, nextBaseColor);
            }

            if (material.HasProperty(ColorId))
            {
                Color nextColor = color;
                nextColor.a = alpha;
                material.SetColor(ColorId, nextColor);
            }
        }

        public static Color GetMaterialColor(Material material, int colorPropertyId)
        {
            if (material == null || !material.HasProperty(colorPropertyId))
            {
                return Color.white;
            }

            return material.GetColor(colorPropertyId);
        }

        private static void ConfigureTransparent(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");

            if (material.HasProperty(SurfaceId)) material.SetFloat(SurfaceId, 1f);
            if (material.HasProperty(BlendId)) material.SetFloat(BlendId, 0f);
            if (material.HasProperty(SrcBlendId)) material.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            if (material.HasProperty(DstBlendId)) material.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty(ZWriteId)) material.SetFloat(ZWriteId, 0f);
            if (material.HasProperty(AlphaClipId)) material.SetFloat(AlphaClipId, 1f);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
