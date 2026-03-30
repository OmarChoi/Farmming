using Photon.Pun;
using UnityEngine;

public class PlayerHelperInteractionAbility : PlayerAbility
{
    [SerializeField] private KeyCode _equipKey = KeyCode.F;
    [SerializeField] private Transform _equipSlot;

    private PlayerTerrainAbility _terrainAbility;
    private HelperController _currentHelper;

    public HelperController CurrentHelper => _currentHelper;
    public Transform EquipSlot => _equipSlot;

    protected override void Awake()
    {
        base.Awake();
        _terrainAbility = _owner.GetAbility<PlayerTerrainAbility>();
    }

    private void OnDestroy()
    {
        if (_currentHelper == null) return;

        _currentHelper.OnActionStarted -= OnHelperActionStarted;
        _currentHelper.OnActionEnded -= OnHelperActionEnded;
        _currentHelper = null;
    }

    private void Update()
    {
        if (!_owner.IsMine) return;
        if (!_owner.CanMove) return;

        if (Input.GetKeyDown(_equipKey))
            ToggleEquip();

        if (_currentHelper != null && _currentHelper.State == EHelperState.Equipped)
        {
            if (Input.GetMouseButtonDown(0))
                TryInteractPrimary();
            else if (Input.GetMouseButtonDown(1))
                TryInteractSecondary();
        }
    }

    public void Summon(HelperController helper)
    {
        if (_currentHelper != null)
            Unsummon();

        _currentHelper = helper;
        _currentHelper.OnActionStarted += OnHelperActionStarted;
        _currentHelper.OnActionEnded += OnHelperActionEnded;
        _currentHelper.Summon(_owner);

        _currentHelper.PhotonView?.RPC(
            nameof(HelperController.RPC_Summon), RpcTarget.Others,
            _owner.PhotonView.ViewID);
    }

    public void Unsummon()
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
        if (_currentHelper == null) return;

        if (_currentHelper.State == EHelperState.Equipped)
        {
            _currentHelper.Unequip();
            _currentHelper.PhotonView?.RPC(
                nameof(HelperController.RPC_Unequip), RpcTarget.Others);
        }
        else
        {
            _currentHelper.Equip(_equipSlot);
            _currentHelper.PhotonView?.RPC(
                nameof(HelperController.RPC_Equip), RpcTarget.Others, _owner.PhotonView.ViewID);
        }
    }

    private void TryInteractPrimary()
    {
        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        _currentHelper.InteractPrimary(cell);
    }

    private void TryInteractSecondary()
    {
        TerrainCell cell = _terrainAbility.GetFrontCell();
        if (cell == null) return;

        _currentHelper.InteractSecondary(cell);
    }
}