using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class PlayerOcclusionAbility : PlayerAbility
{
    [SerializeField] private LayerMask _occlusionLayer;
    [SerializeField] private Vector3 _playerOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float _castRadius = 0.75f;
    [SerializeField, Range(0.1f, 1f)] private float _transparentAlpha = 0.35f;
    [SerializeField] private float _fadeSpeed = 4f;

    private readonly Dictionary<Wood, TreeFadeState> _treeStates = new();
    private readonly HashSet<Wood> _currentHits = new();
    private readonly List<Wood> _removeBuffer = new();
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[32];

    private Camera _mainCamera;
    private Transform _rootTransform;

    private abstract class TreeFadeState
    {
        public abstract void BeginFade(float transparentAlpha);
        public abstract void Tick(float fadeStep);
        public abstract bool IsFullyOpaque { get; }
        public abstract void Dispose();
    }

    private sealed class SingleRendererFadeState : TreeFadeState
    {
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly Renderer _renderer;
        private readonly Material[] _originalMaterials;
        private readonly Material[] _transparentMaterials;
        private float _currentAlpha = 1f;
        private float _targetAlpha = 1f;
        private bool _isUsingTransparentMaterials;

        public SingleRendererFadeState(Renderer renderer, float alpha)
        {
            _renderer = renderer;
            _originalMaterials = renderer != null ? renderer.sharedMaterials : System.Array.Empty<Material>();
            _transparentMaterials = new Material[_originalMaterials.Length];

            for (int i = 0; i < _originalMaterials.Length; i++)
            {
                Material source = _originalMaterials[i];
                if (source == null) continue;

                Material transparent = new Material(source);
                transparent.name = source.name + "_OcclusionRuntime";
                ConfigureTransparentMaterial(transparent, alpha);
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
            SetAlpha(_currentAlpha);

            if (Mathf.Approximately(_currentAlpha, 1f) && Mathf.Approximately(_targetAlpha, 1f))
            {
                _renderer.sharedMaterials = _originalMaterials;
                _isUsingTransparentMaterials = false;
            }
        }

        public override bool IsFullyOpaque => Mathf.Approximately(_currentAlpha, 1f) && Mathf.Approximately(_targetAlpha, 1f);

        public override void Dispose()
        {
            if (_renderer != null)
            {
                _renderer.sharedMaterials = _originalMaterials;
            }

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
            SetAlpha(_currentAlpha);
        }

        private void SetAlpha(float alpha)
        {
            for (int i = 0; i < _transparentMaterials.Length; i++)
            {
                Material material = _transparentMaterials[i];
                if (material == null) continue;

                SetAlpha(material, BaseColorId, alpha);
                SetAlpha(material, ColorId, alpha);
            }
        }

        private static void ConfigureTransparentMaterial(Material material, float alpha)
        {
            material.SetOverrideTag("RenderType", "Transparent");

            if (material.HasProperty(SurfaceId)) material.SetFloat(SurfaceId, 1f);
            if (material.HasProperty(BlendId)) material.SetFloat(BlendId, 0f);
            if (material.HasProperty(SrcBlendId)) material.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            if (material.HasProperty(DstBlendId)) material.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty(ZWriteId)) material.SetFloat(ZWriteId, 0f);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;

            SetAlpha(material, BaseColorId, alpha);
            SetAlpha(material, ColorId, alpha);
        }

        private static void SetAlpha(Material material, int colorPropertyId, float alpha)
        {
            if (!material.HasProperty(colorPropertyId)) return;

            Color color = material.GetColor(colorPropertyId);
            color.a = alpha;
            material.SetColor(colorPropertyId, color);
        }
    }

    protected override void Awake()
    {
        base.Awake();
        _rootTransform = transform.root;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        RestoreAll();
        DisposeAll();
    }

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (_owner == null || !_owner.IsMine) return;

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        Vector3 cameraPosition = _mainCamera.transform.position;
        Vector3 playerPosition = _owner.transform.position + _playerOffset;
        Vector3 direction = playerPosition - cameraPosition;
        float distance = direction.magnitude;

        _currentHits.Clear();

        if (distance > 0.1f)
        {
            CollectHitTrees(cameraPosition, direction / distance, distance);
        }

        UpdateTreeTransparency();
    }

    private void CollectHitTrees(Vector3 origin, Vector3 direction, float distance)
    {
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            _castRadius,
            direction,
            _hitBuffer,
            distance,
            _occlusionLayer,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = _hitBuffer[i].collider;
            _hitBuffer[i] = default;

            if (collider == null) continue;
            if (collider.transform.IsChildOf(_rootTransform)) continue;

            Wood tree = collider.GetComponentInParent<Wood>();
            if (tree == null) continue;
            if (tree.transform.IsChildOf(_rootTransform)) continue;

            _currentHits.Add(tree);
        }
    }

    private void UpdateTreeTransparency()
    {
        foreach (Wood tree in _currentHits)
        {
            SetTreeTransparent(tree);
        }

        _removeBuffer.Clear();

        foreach (KeyValuePair<Wood, TreeFadeState> pair in _treeStates)
        {
            Wood tree = pair.Key;
            TreeFadeState state = pair.Value;

            if (tree == null)
            {
                _removeBuffer.Add(tree);
                continue;
            }

            float targetAlpha = _currentHits.Contains(tree) ? _transparentAlpha : 1f;
            state.BeginFade(targetAlpha);
            state.Tick(_fadeSpeed * Time.deltaTime);

            if (!_currentHits.Contains(tree) && state.IsFullyOpaque)
            {
                _removeBuffer.Add(tree);
            }
        }

        for (int i = 0; i < _removeBuffer.Count; i++)
        {
            Wood tree = _removeBuffer[i];
            if (tree != null && _treeStates.TryGetValue(tree, out TreeFadeState state))
            {
                state.Dispose();
            }

            _treeStates.Remove(tree);
        }

        _removeBuffer.Clear();
    }

    private void SetTreeTransparent(Wood tree)
    {
        if (tree == null) return;
        if (_treeStates.ContainsKey(tree)) return;

        Renderer[] renderers = tree.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        TreeFadeState state;

        if (renderers.Length == 1)
        {
            state = new SingleRendererFadeState(renderers[0], _transparentAlpha);
        }
        else
        {
            state = new TreeGroupFadeState(renderers, _transparentAlpha);
        }

        _treeStates.Add(tree, state);
    }

    private void RestoreAll()
    {
        foreach (KeyValuePair<Wood, TreeFadeState> pair in _treeStates)
        {
            pair.Value.Dispose();
        }

        _treeStates.Clear();
        _currentHits.Clear();
        _removeBuffer.Clear();
    }

    private void DisposeAll()
    {
        RestoreAll();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RestoreAll();
        DisposeAll();
        _mainCamera = null;
    }

    private sealed class TreeGroupFadeState : TreeFadeState
    {
        private readonly TreeFadeState[] _states;

        public TreeGroupFadeState(Renderer[] renderers, float alpha)
        {
            _states = new TreeFadeState[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                _states[i] = new SingleRendererFadeState(renderers[i], alpha);
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

        public override void Dispose()
        {
            for (int i = 0; i < _states.Length; i++)
            {
                _states[i].Dispose();
            }
        }
    }
}
