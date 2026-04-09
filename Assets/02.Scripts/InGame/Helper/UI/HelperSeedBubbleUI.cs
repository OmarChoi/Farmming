using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HelperSeedBubbleUI : MonoBehaviour
{
    [Header("Bubble")]
    [SerializeField] private GameObject _bubbleRoot;
    [SerializeField] private Image _seedIcon;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private float _verticalOffset = 0.55f;
    [SerializeField] private float _sideOffset = 0.85f;
    [SerializeField] private float _forwardOffset = 0.05f;

    private SeedSelectAbility _seedSelectAbility;
    private HelperController _helperController;
    private Camera _mainCamera;
    private bool _lastCanShowBubble;

    private void Awake()
    {
        _helperController = GetComponent<HelperController>() ?? GetComponentInParent<HelperController>();

        if (_bubbleRoot != null)
            _bubbleRoot.SetActive(false);
    }

    private void Start()
    {
        _seedSelectAbility = GetComponent<SeedSelectAbility>() ?? GetComponentInParent<SeedSelectAbility>();

        if (_seedSelectAbility != null)
        {
            _seedSelectAbility.OnSeedSelected += RefreshUI;
            RefreshUI(_seedSelectAbility.SelectedSeed);
        }
    }

    private void OnDestroy()
    {
        if (_seedSelectAbility != null)
        {
            _seedSelectAbility.OnSeedSelected -= RefreshUI;
        }
    }

    private void LateUpdate()
    {
        bool canShowBubble = CanShowBubble();
        if (canShowBubble != _lastCanShowBubble)
        {
            RefreshUI(_seedSelectAbility != null ? _seedSelectAbility.SelectedSeed : null);
            _lastCanShowBubble = canShowBubble;
        }

        if (_bubbleRoot == null || !_bubbleRoot.activeSelf)
            return;

        if (_mainCamera == null)
            _mainCamera = Camera.main;
        if (_mainCamera == null)
            return;

        UpdateBubbleTransform();
        _bubbleRoot.transform.rotation = _mainCamera.transform.rotation;
    }

    private void RefreshUI(SeedItemDataSO seed)
    {
        int count = _seedSelectAbility != null ? _seedSelectAbility.SelectedSeedCount : 0;
        bool shouldShow = seed != null && CanShowBubble();

        if (!shouldShow)
        {
            if (_bubbleRoot != null)
                _bubbleRoot.SetActive(false);
            return;
        }

        _bubbleRoot.SetActive(true);
        if (_seedIcon != null)
        {
            _seedIcon.sprite = seed.Icon;
            _seedIcon.enabled = seed.Icon != null;
        }

        if (_countText != null)
            _countText.text = $"x{count}";
    }

    private bool CanShowBubble()
    {
        return _helperController != null && _helperController.State == EHelperState.Equipped;
    }

    private void OnValidate()
    {
        if (_bubbleRoot == null || _seedIcon == null || _countText == null)
            return;

        if (_bubbleRoot.activeSelf)
            _bubbleRoot.SetActive(false);
    }

    private void UpdateBubbleTransform()
    {
        if (_helperController == null || _bubbleRoot == null || _mainCamera == null)
            return;

        Vector3 horizontalCameraRight = Vector3.ProjectOnPlane(_mainCamera.transform.right, Vector3.up);
        if (horizontalCameraRight.sqrMagnitude < 0.0001f)
            horizontalCameraRight = _mainCamera.transform.right;

        horizontalCameraRight.Normalize();

        Vector3 towardCamera = Vector3.ProjectOnPlane(_mainCamera.transform.forward, Vector3.up);
        if (towardCamera.sqrMagnitude > 0.0001f)
            towardCamera.Normalize();

        Vector3 anchorPosition = _helperController.transform.position + Vector3.up * _verticalOffset;
        Vector3 worldOffset = horizontalCameraRight * _sideOffset - towardCamera * _forwardOffset;

        _bubbleRoot.transform.position = anchorPosition + worldOffset;
    }
}
