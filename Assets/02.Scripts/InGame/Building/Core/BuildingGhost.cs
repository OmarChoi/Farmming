using System.Collections.Generic;
using UnityEngine;

public class BuildingGhost
{
    private GameObject _instance;
    private Material _validMaterial;
    private Material _invalidMaterial;

    // 원본 머티리얼 캐싱 (Renderer별)
    // todo. _originalMaterials를 블렌딩 셰이더에 원본 텍스처로 전달
    private readonly List<(Renderer renderer, Material[] originals)> _originalMaterials = new List<(Renderer, Material[])>();

    // todo. 높이 기반 블렌딩 셰이더 적용 - UV.y/월드 높이 기준 cutoff로 Ghost/완성 머티리얼 전환
    // todo. SetProgress(float progress) - 건설 진행도(0=Ghost, 1=완성)에 따른 시각 효과

    private Renderer[] _cachedRenderers;
    private Material[][] _cachedGhostMaterials;
    // 중복 SetValid 호출 방지
    private bool? _lastValid;

    public GameObject Instance => _instance;

    public void Spawn(GameObject prefab, Material validMaterial, Material invalidMaterial, Transform parent = null)
    {
        _validMaterial = validMaterial;
        _invalidMaterial = invalidMaterial;
        _lastValid = null;

        _instance = Object.Instantiate(prefab, parent);
        _originalMaterials.Clear();

        // Renderer 캐싱
        _cachedRenderers = _instance.GetComponentsInChildren<Renderer>();
        _cachedGhostMaterials = new Material[_cachedRenderers.Length][];

        for (int r = 0; r < _cachedRenderers.Length; r++)
        {
            var renderer = _cachedRenderers[r];
            _originalMaterials.Add((renderer, renderer.sharedMaterials));

            // Renderer별 Ghost 머티리얼 배열 캐싱 (재할당 없이 내용만 교체)
            int matCount = renderer.sharedMaterials.Length;
            _cachedGhostMaterials[r] = new Material[matCount];
            for (int i = 0; i < matCount; i++)
            {
                _cachedGhostMaterials[r][i] = _validMaterial;
            }
            renderer.materials = _cachedGhostMaterials[r];
        }

        // Ghost 건물이 다른 오브젝트와 충돌하지 않게 Collider 끄기 (설치 완료되면 킨다.)
        var colliders = _instance.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
    }

    public void UpdateTransform(Vector3 position, Quaternion rotation)
    {
        if (_instance == null) return;
        _instance.transform.SetPositionAndRotation(position, rotation);
    }

    public void SetValid(bool isValid)
    {
        if (_instance == null) return;
        if (_lastValid.HasValue && _lastValid.Value == isValid) return;
        _lastValid = isValid;

        Material mat = isValid ? _validMaterial : _invalidMaterial;
        for (int r = 0; r < _cachedRenderers.Length; r++)
        {
            var mats = _cachedGhostMaterials[r];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = mat;
            }
            _cachedRenderers[r].materials = mats;
        }
    }

    public void SetVisible(bool visible)
    {
        if (_instance == null) return;
        _instance.SetActive(visible);
    }

    public void Destroy()
    {
        if (_instance != null)
        {
            Object.Destroy(_instance);
            _instance = null;
        }
        _originalMaterials.Clear();
        _cachedRenderers = null;
        _cachedGhostMaterials = null;
        _lastValid = null;
    }
}
