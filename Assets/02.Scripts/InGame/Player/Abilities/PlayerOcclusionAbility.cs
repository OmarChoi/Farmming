using System.Collections.Generic;
using UnityEngine;

public class PlayerOcclusionAbility : PlayerAbility
{
    [SerializeField] private float _fadeRadius = 0.12f;
    [SerializeField] private float _castRadius = 0.5f;
    [SerializeField] private float _candidateSearchPadding = 2f;
    [SerializeField] private float _fadeStrengthLerpSpeed = 8f;
    [SerializeField, Range(0f, 1f)] private float _ditherMinVisibility = 0.2f;
    [SerializeField] private LayerMask _occlusionLayer;
    [SerializeField] private Vector3 _playerOffset = Vector3.up;

    private static readonly int OcclusionScreenPos = Shader.PropertyToID("_OcclusionDitherScreenPos");
    private static readonly int OcclusionRadiusProp = Shader.PropertyToID("_OcclusionDitherRadius");
    private static readonly int OcclusionStrengthProp = Shader.PropertyToID("_OcclusionDitherStrength");
    private static readonly int OcclusionMinVisibilityProp = Shader.PropertyToID("_OcclusionDitherMinVisibility");

    private Camera _mainCamera;
    private MaterialPropertyBlock _block;
    private readonly Dictionary<Renderer, float> _occluderStrengths = new();
    private readonly HashSet<Renderer> _currentFrameOccluders = new();
    private readonly List<Renderer> _rendererBuffer = new();
    private readonly List<Renderer> _candidateRenderers = new();
    private readonly Collider[] _overlapBuffer = new Collider[32];

    protected override void Awake()
    {
        base.Awake();
        _block = new MaterialPropertyBlock();
    }

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (_owner == null) return;
        if (!_owner.IsMine) return;
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            return;
        }

        Vector3 playerPos = _owner.transform.position + _playerOffset;
        Vector3 cameraPos = _mainCamera.transform.position;
        Vector3 dir = playerPos - cameraPos;
        float dist = dir.magnitude;

        _currentFrameOccluders.Clear();

        if (dist >= 0.1f)
        {
            CollectCandidateRenderers(playerPos, dist);
            CollectOccluders(playerPos, dist);
        }

        UpdateOccluders();
    }

    private void CollectCandidateRenderers(Vector3 playerPos, float playerDistance)
    {
        _candidateRenderers.Clear();

        float searchRadius = Mathf.Max(_castRadius, playerDistance + _candidateSearchPadding);
        int overlapCount = Physics.OverlapSphereNonAlloc(
            playerPos,
            searchRadius,
            _overlapBuffer,
            _occlusionLayer,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < overlapCount; i++)
        {
            Collider collider = _overlapBuffer[i];
            if (collider == null) continue;

            AddCandidateRenderers(collider);
            _overlapBuffer[i] = null;
        }
    }

    private void AddCandidateRenderers(Collider collider)
    {
        Transform scope = collider.transform;
        while (scope.parent != null && ((_occlusionLayer.value & (1 << scope.parent.gameObject.layer)) != 0))
        {
            scope = scope.parent;
        }

        Renderer[] renderers = scope.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;
            if (renderer.transform.IsChildOf(transform.root)) continue;
            if (((1 << renderer.gameObject.layer) & _occlusionLayer.value) == 0) continue;
            if (!SupportsOcclusion(renderer)) continue;
            if (_candidateRenderers.Contains(renderer)) continue;

            _candidateRenderers.Add(renderer);
        }
    }

    private void CollectOccluders(Vector3 playerPos, float dist)
    {
        Vector3 vp = _mainCamera.WorldToViewportPoint(playerPos);
        Vector4 screenPos = new Vector4(vp.x, vp.y, 0f, 0f);

        for (int i = 0; i < _candidateRenderers.Count; i++)
        {
            Renderer renderer = _candidateRenderers[i];
            if (renderer == null) continue;
            if (!renderer.enabled) continue;
            if (!IsRendererOccluding(renderer, vp, dist)) continue;

            _currentFrameOccluders.Add(renderer);

            if (!_occluderStrengths.ContainsKey(renderer))
            {
                _occluderStrengths.Add(renderer, 0f);
            }

            renderer.GetPropertyBlock(_block);
            _block.SetVector(OcclusionScreenPos, screenPos);
            _block.SetFloat(OcclusionMinVisibilityProp, _ditherMinVisibility);
            renderer.SetPropertyBlock(_block);
        }
    }

    private bool IsRendererOccluding(Renderer renderer, Vector3 playerViewportPos, float playerDistance)
    {
        Bounds bounds = renderer.bounds;
        Vector3 centerViewportPos = _mainCamera.WorldToViewportPoint(bounds.center);

        if (centerViewportPos.z <= 0f || centerViewportPos.z >= playerDistance)
        {
            return false;
        }

        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        bool hasVisibleCorner = false;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 cornerViewportPos = _mainCamera.WorldToViewportPoint(corner);
                    if (cornerViewportPos.z <= 0f) continue;

                    hasVisibleCorner = true;
                    minX = Mathf.Min(minX, cornerViewportPos.x);
                    maxX = Mathf.Max(maxX, cornerViewportPos.x);
                    minY = Mathf.Min(minY, cornerViewportPos.y);
                    maxY = Mathf.Max(maxY, cornerViewportPos.y);
                }
            }
        }

        if (!hasVisibleCorner)
        {
            return false;
        }

        float viewportRadiusY = _castRadius / Mathf.Max(playerDistance, 0.001f);
        float viewportRadiusX = viewportRadiusY / Mathf.Max(_mainCamera.aspect, 0.001f);

        return playerViewportPos.x >= minX - viewportRadiusX
            && playerViewportPos.x <= maxX + viewportRadiusX
            && playerViewportPos.y >= minY - viewportRadiusY
            && playerViewportPos.y <= maxY + viewportRadiusY;
    }

    private static bool SupportsOcclusion(Renderer renderer)
    {
        Material[] materials = renderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null) continue;
            if (!material.HasProperty(OcclusionRadiusProp)) continue;
            if (!material.HasProperty(OcclusionScreenPos)) continue;
            if (!material.HasProperty(OcclusionStrengthProp)) continue;
            return true;
        }

        return false;
    }

    private void UpdateOccluders()
    {
        if (_occluderStrengths.Count == 0) return;

        _rendererBuffer.Clear();
        _rendererBuffer.AddRange(_occluderStrengths.Keys);

        float lerpFactor = 1f - Mathf.Exp(-_fadeStrengthLerpSpeed * Time.deltaTime);

        for (int i = 0; i < _rendererBuffer.Count; i++)
        {
            Renderer renderer = _rendererBuffer[i];
            if (renderer == null)
            {
                _occluderStrengths.Remove(renderer);
                continue;
            }

            float currentStrength = _occluderStrengths[renderer];
            float targetStrength = _currentFrameOccluders.Contains(renderer) ? 1f : 0f;
            float nextStrength = Mathf.Lerp(currentStrength, targetStrength, lerpFactor);

            if (Mathf.Abs(nextStrength - targetStrength) < 0.001f)
            {
                nextStrength = targetStrength;
            }

            renderer.GetPropertyBlock(_block);
            _block.SetFloat(OcclusionRadiusProp, nextStrength > 0f ? _fadeRadius : 0f);
            _block.SetFloat(OcclusionStrengthProp, nextStrength);
            _block.SetFloat(OcclusionMinVisibilityProp, _ditherMinVisibility);
            renderer.SetPropertyBlock(_block);

            if (targetStrength <= 0f && nextStrength <= 0f)
            {
                _occluderStrengths.Remove(renderer);
                continue;
            }

            _occluderStrengths[renderer] = nextStrength;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Camera targetCamera = _mainCamera != null ? _mainCamera : Camera.main;
        if (_owner == null || targetCamera == null) return;

        Vector3 playerPos = _owner.transform.position + _playerOffset;
        Vector3 cameraPos = targetCamera.transform.position;
        Vector3 dir = playerPos - cameraPos;
        float dist = dir.magnitude;

        if (dist < 0.001f) return;

        Vector3 dirNorm = dir / dist;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(cameraPos, _castRadius);
        Gizmos.DrawWireSphere(playerPos, _castRadius);

        Vector3 right = Vector3.Cross(dirNorm, Vector3.up);
        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.Cross(dirNorm, Vector3.forward);
        }
        right.Normalize();

        Vector3 up = Vector3.Cross(right, dirNorm).normalized;

        Gizmos.DrawLine(cameraPos + right * _castRadius, playerPos + right * _castRadius);
        Gizmos.DrawLine(cameraPos - right * _castRadius, playerPos - right * _castRadius);
        Gizmos.DrawLine(cameraPos + up * _castRadius, playerPos + up * _castRadius);
        Gizmos.DrawLine(cameraPos - up * _castRadius, playerPos - up * _castRadius);

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawLine(cameraPos, playerPos);
    }
}
