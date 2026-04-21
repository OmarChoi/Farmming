using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InteractionPrompt : MonoBehaviour
{
    public static UI_InteractionPrompt Instance { get; private set; }

    [Header("루트")]
    [SerializeField] private GameObject _root;

    [Header("UI 위치")]
    [SerializeField] private RectTransform _panel;

    private Transform _target;
    private Vector3 _offset;

    private Camera _mainCamera;

    private void Awake()
    {
        Instance = this;
        _mainCamera = Camera.main;
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void LateUpdate()
    {
        if (_root == null || !_root.activeSelf) return;
        if (_target == null)
        {
            Hide();
            return;
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera == null) return;

        Vector3 worldPosition = _target.position + _offset;
        Vector3 screenPosition = _mainCamera.WorldToScreenPoint(worldPosition);

        // 카메라 뒤면 숨깁니다.
        if (screenPosition.z < 0f)
        {
            _root.SetActive(false);
            return;
        }

        _panel.position = screenPosition;
    }

    public void Show(Transform target, Vector3 offset)
    {
        _target = target;
        _offset = offset;

        if (_root != null && !_root.activeSelf)
        {
            _root.SetActive(true);
        }
    }

    public void Hide()
    {
        _target = null;

        if (_root != null && _root.activeSelf)
        {
            _root.SetActive(false);
        }
    }
}
