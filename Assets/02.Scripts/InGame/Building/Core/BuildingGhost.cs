using System.Collections.Generic;
using UnityEngine;

public class BuildingGhost
{
    private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

    private GameObject _instance;
    private Color _validColor;
    private Color _invalidColor;

    // 원본 머티리얼 캐싱 (Renderer별)
    // todo. _originalMaterials를 블렌딩 셰이더에 원본 텍스처로 전달
    private readonly List<(Renderer renderer, Material[] originals)> _originalMaterials = new List<(Renderer, Material[])>();

    // todo. 높이 기반 블렌딩 셰이더 적용 - UV.y/월드 높이 기준 cutoff로 Ghost/완성 머티리얼 전환
    // todo. SetProgress(float progress) - 건설 진행도(0=Ghost, 1=완성)에 따른 시각 효과

    private Renderer[] _cachedRenderers;
    private readonly MaterialPropertyBlock _propBlock = new MaterialPropertyBlock();
    // 중복 SetValid 호출 방지
    private bool? _lastValid;

    public GameObject Instance => _instance;

    public void Spawn(GameObject prefab, Material ghostMaterial, Color validColor, Color invalidColor, Transform parent = null)
    {
        _validColor = validColor;
        _invalidColor = invalidColor;
        _lastValid = null;

        _instance = Object.Instantiate(prefab, parent);
        _originalMaterials.Clear();

        // Renderer 캐싱 + Ghost 머티리얼로 교체 (sharedMaterial 사용, 인스턴스 생성 없음)
        _cachedRenderers = _instance.GetComponentsInChildren<Renderer>();
        foreach (var renderer in _cachedRenderers)
        {
            _originalMaterials.Add((renderer, renderer.sharedMaterials));

            var ghostMats = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < ghostMats.Length; i++)
            {
                ghostMats[i] = ghostMaterial;
            }
            renderer.sharedMaterials = ghostMats;
        }
        
        // Ghost 건물이 다른 오브젝트와 충돌하지 않게 Collider 끄기
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

        _propBlock.SetColor(ColorId, isValid ? _validColor : _invalidColor);
        foreach (var renderer in _cachedRenderers)
        {
            renderer.SetPropertyBlock(_propBlock);
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
        _lastValid = null;
    }
}
