using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GroundSelectAbility : HelperAbility
{
    public event Action<ItemDataSO> OnGroundSelected;

    [Header("Bubble")]
    [SerializeField] private GameObject _bubbleRoot;
    [SerializeField] private Image _groundIcon;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private float _verticalOffset = 0.55f;
    [SerializeField] private float _sideOffset = 2.05f;
    [SerializeField] private float _forwardOffset = 0.05f;

    private List<(int slotIndex, ItemDataSO item)> _availableGrounds = new();
    private int _selectedIndex = -1;
    private PlayerInventoryAbility _inventory;
    private GroundActionAbility _groundActionAbility;
    private HelperController _helperController;
    private Camera _mainCamera;
    private bool _lastCanShowBubble;

    public ItemDataSO SelectedGround =>
        _selectedIndex >= 0 && _selectedIndex < _availableGrounds.Count
            ? _availableGrounds[_selectedIndex].item
            : null;

    public int SelectedGroundSlotIndex =>
        _selectedIndex >= 0 && _selectedIndex < _availableGrounds.Count
            ? _availableGrounds[_selectedIndex].slotIndex
            : -1;

    public int SelectedGroundCount => GetGroundCount(SelectedGround);
    public bool HasSelectedGroundAvailable => SelectedGround != null && SelectedGroundCount > 0;

    protected override void Awake()
    {
        base.Awake();
        _helperController = GetComponent<HelperController>() ?? GetComponentInParent<HelperController>();
        _groundActionAbility = GetComponent<GroundActionAbility>() ?? GetComponentInParent<GroundActionAbility>();

        if (_bubbleRoot != null)
            _bubbleRoot.SetActive(false);
    }

    private void Start()
    {
        _inventory = _owner.PlayerOwner?.GetAbility<PlayerInventoryAbility>();
        if (_inventory != null)
        {
            _inventory.OnSlotChanged += OnInventoryChanged;
        }

        RefreshGrounds();
    }

    private void OnDestroy()
    {
        if (_inventory != null)
        {
            _inventory.OnSlotChanged -= OnInventoryChanged;
        }
    }

    private void LateUpdate()
    {
        bool canShowBubble = CanShowBubble();
        if (canShowBubble != _lastCanShowBubble)
        {
            RefreshBubbleUI(SelectedGround);
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

    private void OnInventoryChanged(int _)
    {
        RefreshGrounds();
    }

    private void Update()
    {
        if (_availableGrounds.Count == 0) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll < 0f)
            SelectNext();
        else if (scroll > 0f)
            SelectPrev();
    }

    private void SelectNext()
    {
        if (_availableGrounds.Count == 0) return;
        _selectedIndex = (_selectedIndex + 1) % _availableGrounds.Count;
        OnGroundSelected?.Invoke(SelectedGround);
    }

    private void SelectPrev()
    {
        if (_availableGrounds.Count == 0) return;
        _selectedIndex = (_selectedIndex - 1 + _availableGrounds.Count) % _availableGrounds.Count;
        NotifySelectionChanged();
    }

    public bool TrySelectGround(ItemDataSO groundItem)
    {
        if (groundItem == null || _inventory == null)
            return false;

        int selectedGroundIndex = _availableGrounds.FindIndex(entry => entry.item == groundItem);
        if (selectedGroundIndex < 0 || GetGroundCount(groundItem) <= 0)
            return false;

        _selectedIndex = selectedGroundIndex;
        NotifySelectionChanged();
        return true;
    }

    private void RefreshGrounds()
    {
        if (_inventory == null)
        {
            _selectedIndex = -1;
            NotifySelectionChanged();
            return;
        }

        _availableGrounds.Clear();
        for (int i = 0; i < _inventory.SlotCount; i++)
        {
            InventorySlot slot = _inventory.GetSlot(i);
            if (slot == null || slot.IsEmpty) continue;

            if (CanUseGroundItem(slot.Item))
            {
                _availableGrounds.Add((i, slot.Item));
            }
        }

        if (_selectedIndex >= _availableGrounds.Count)
            _selectedIndex = _availableGrounds.Count - 1;

        if (_selectedIndex < 0 && _availableGrounds.Count > 0)
            _selectedIndex = 0;

        NotifySelectionChanged();
    }

    private bool CanUseGroundItem(ItemDataSO item)
    {
        if (item == null)
            return false;

        if (_groundActionAbility == null)
            return item.IsGround;

        return _groundActionAbility.CanUseGroundItem(item);
    }

    private int GetGroundCount(ItemDataSO groundItem)
    {
        if (groundItem == null || _inventory == null)
            return 0;

        return _inventory.GetItemCount(groundItem);
    }

    private void NotifySelectionChanged()
    {
        ItemDataSO selectedGround = SelectedGround;
        OnGroundSelected?.Invoke(selectedGround);
        RefreshBubbleUI(selectedGround);
    }

    private void RefreshBubbleUI(ItemDataSO groundItem)
    {
        if (_bubbleRoot == null)
            return;

        int count = SelectedGroundCount;
        bool shouldShow = groundItem != null && count > 0 && CanShowBubble();
        _bubbleRoot.SetActive(shouldShow);

        if (!shouldShow)
            return;

        if (_groundIcon != null)
        {
            _groundIcon.sprite = groundItem.Icon;
            _groundIcon.enabled = groundItem.Icon != null;
        }

        if (_countText != null)
            _countText.text = $"x{count}";
    }

    private bool CanShowBubble()
    {
        return _helperController != null
               && _helperController.State == EHelperState.Equipped
               && !_helperController.IsActing;
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
