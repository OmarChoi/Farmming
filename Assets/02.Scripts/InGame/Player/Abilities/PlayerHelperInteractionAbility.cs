using Photon.Pun;
using UnityEngine;

public class PlayerHelperInteractionAbility : PlayerAbility
{
    [SerializeField] private KeyCode _equipKey = KeyCode.F;
    [SerializeField] private Transform _equipSlot;
    [SerializeField] private Transform _backEquipSlot;

    private PlayerTerrainAbility _terrainAbility;
    private HelperController _currentHelper;
    private HelperController _backHelper;

    public HelperController CurrentHelper => _currentHelper;
    public HelperController BackHelper => _backHelper;
    public Transform EquipSlot => _equipSlot;

    protected override void Awake()
    {
        base.Awake();
        _terrainAbility = _owner.GetAbility<PlayerTerrainAbility>();
    }

    private void OnDestroy()
    {
        if (_currentHelper != null)
        {
            _currentHelper.OnActionStarted -= OnHelperActionStarted;
            _currentHelper.OnActionEnded -= OnHelperActionEnded;
            _currentHelper = null;
        }

        _backHelper = null;
    }

    private void Update()
    {
        if (!_owner.IsMine) return;
        if (!_owner.CanMove) return;

        if (Input.GetKeyDown(_equipKey))
            ToggleEquip();

        HelperController activeHelper = _currentHelper;
        if (activeHelper != null && activeHelper.State == EHelperState.Equipped)
        {
            if (Input.GetMouseButtonDown(0))
                TryInteractPrimary();
            else if (Input.GetMouseButtonDown(1))
                TryInteractSecondary();
        }
    }

    public void Summon(HelperController helper)
    {
        bool isLightHelper = helper.GetAbility<LightActionAbility>() != null;

        if (isLightHelper)
        {
            if (_currentHelper != null)
            {
                return;
            }

            if (_backHelper != null)
                UnsummonBack();

            _backHelper = helper;
            _backHelper.Summon(_owner);

            _backHelper.PhotonView.RpcSafe(
                nameof(HelperController.RPC_Summon), RpcTarget.Others,
                _owner.PhotonView.ViewID);
        }
        else
        {
            if (_backHelper != null && _backHelper.State != EHelperState.Equipped)
            {
                return;
            }

            if (_currentHelper != null)
                Unsummon();

            _currentHelper = helper;
            _currentHelper.OnActionStarted += OnHelperActionStarted;
            _currentHelper.OnActionEnded += OnHelperActionEnded;
            _currentHelper.Summon(_owner);

            _currentHelper.PhotonView.RpcSafe(
                nameof(HelperController.RPC_Summon), RpcTarget.Others,
                _owner.PhotonView.ViewID);
        }
    }

    public void Unsummon()
    {
        if (_currentHelper == null && _backHelper != null)
        {
            UnsummonBack();
            return;
        }

        UnsummonCurrentOnly();
    }

    public void UnsummonCurrentOnly()
    {
        if (_currentHelper == null) return;

        _currentHelper.OnActionStarted -= OnHelperActionStarted;
        _currentHelper.OnActionEnded -= OnHelperActionEnded;

        if (_currentHelper.IsActing)
        {
            _owner.AllowCameraRotation = false;
            _owner.UnlockAction();
        }

        if (_currentHelper.State == EHelperState.Equipped)
            _currentHelper.Unequip();

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Destroy(_currentHelper.gameObject);
        else
            Destroy(_currentHelper.gameObject);

        _currentHelper = null;
    }

    public void UnsummonBack()
    {
        if (_backHelper == null) return;

        if (_backHelper.State == EHelperState.Equipped)
            _backHelper.Unequip();

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Destroy(_backHelper.gameObject);
        else
            Destroy(_backHelper.gameObject);

        _backHelper = null;
    }

    private void OnHelperActionStarted()
    {
        _owner.AllowCameraRotation = true;
        _owner.LockAction();
    }

    private void OnHelperActionEnded()
    {
        _owner.AllowCameraRotation = false;
        _owner.UnlockAction();
    }

    private void ToggleEquip()
    {
        if (_currentHelper != null)
        {
            if (_currentHelper.State == EHelperState.Equipped)
            {
                _currentHelper.Unequip();
                _currentHelper.PhotonView.RpcSafe(
                    nameof(HelperController.RPC_Unequip), RpcTarget.Others);
            }
            else
            {
                _currentHelper.Equip(_equipSlot);
                _currentHelper.PhotonView.RpcSafe(
                    nameof(HelperController.RPC_Equip), RpcTarget.Others, _owner.PhotonView.ViewID);
            }
            return;
        }

        if (_backHelper != null)
        {
            Transform slot = _backEquipSlot != null ? _backEquipSlot : _equipSlot;
            if (_backHelper.State == EHelperState.Equipped)
            {
                _backHelper.Unequip();
                _backHelper.PhotonView.RpcSafe(
                    nameof(HelperController.RPC_Unequip), RpcTarget.Others);
            }
            else
            {
                _backHelper.Equip(slot);
                _backHelper.PhotonView.RpcSafe(
                    nameof(HelperController.RPC_Equip), RpcTarget.Others, _owner.PhotonView.ViewID);
            }
        }
    }

    private void TryInteractPrimary()
    {
        TerrainCell cell = _terrainAbility.GetFrontCell(out bool isBelowFallback);
        if (cell == null) return;
        if (isBelowFallback) return; // 아래 셀은 파기 대상 아님

        _currentHelper.InteractPrimary(cell);
    }

    private void TryInteractSecondary()
    {
        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        _currentHelper.InteractSecondary(cell);
    }
}