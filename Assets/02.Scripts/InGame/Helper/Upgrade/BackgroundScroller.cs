using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BackgroundScroller : MonoBehaviour
{
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int LegacyMainTexId = Shader.PropertyToID("_MainTex");

    public Vector2 Direction = Vector2.right;
    public float Speed = 1f;

    private Material _materialInstance;
    private Vector2 _currentOffset;
    private int _texturePropertyId = -1;

    private void Awake()
    {
        // .material 접근 시 자동으로 인스턴스 생성됨 → 에셋 오염 방지
        _materialInstance = GetComponent<Renderer>().material;
        _texturePropertyId = GetTexturePropertyId(_materialInstance);

        if (_texturePropertyId == -1) return;
        _currentOffset = _materialInstance.GetTextureOffset(_texturePropertyId);
    }

    private void Update()
    {
        if (_texturePropertyId == -1) return;
        if (Direction.sqrMagnitude <= Mathf.Epsilon || Mathf.Approximately(Speed, 0f)) return;

        _currentOffset += Direction.normalized * (Speed * Time.deltaTime);

        // float 정밀도 보호 (UV는 1.0 주기)
        _currentOffset.x -= Mathf.Floor(_currentOffset.x);
        _currentOffset.y -= Mathf.Floor(_currentOffset.y);

        _materialInstance.SetTextureOffset(_texturePropertyId, _currentOffset);
    }

    private void OnDestroy()
    {
        if (_materialInstance != null)
            Destroy(_materialInstance);
    }

    private static int GetTexturePropertyId(Material material)
    {
        if (material == null) return -1;
        if (material.HasProperty(BaseMapId)) return BaseMapId;
        if (material.HasProperty(LegacyMainTexId)) return LegacyMainTexId;
        return -1;
    }
}
