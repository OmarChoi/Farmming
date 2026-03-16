using System;
using UnityEngine;

public class PlayerInventoryAbility : PlayerAbility
{
    private InventoryDomain _inventory;
    private bool _isOpen;

    public InventoryDomain Inventory => _inventory;
    public bool IsOpen => _isOpen;

    public event Action<bool> OnToggle;

    /// 로컬 플레이어의 InventoryAbility가 생성되면 발생.
    /// UI_Inventory가 이 이벤트를 구독하여 바인딩한다.
    public static event Action<PlayerInventoryAbility> OnLocalPlayerReady;

    protected override void Awake()
    {
        base.Awake();
        _inventory = new InventoryDomain();
    }

    private void Start()
    {
        // TODO: PUN2 도입 후 PhotonView.IsMine 체크 추가
        OnLocalPlayerReady?.Invoke(this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            Toggle();
    }

    public void Toggle()
    {
        _isOpen = !_isOpen;
        _owner.IsUIOpen = _isOpen;
        SetCursorLock(!_isOpen);
        OnToggle?.Invoke(_isOpen);
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        _owner.IsUIOpen = true;
        SetCursorLock(false);
        OnToggle?.Invoke(true);
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _owner.IsUIOpen = false;
        SetCursorLock(true);
        OnToggle?.Invoke(false);
    }

    private void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}