using Photon.Pun;
using UnityEngine;

public class PlayerHelperInteractionAbility : PlayerAbility
{
    [SerializeField] private KeyCode _equipKey = KeyCode.F;
    [SerializeField] private Transform _equipSlot;
    [SerializeField] private Transform _backEquipSlot;
    [SerializeField] private HelperPlayerAnimationMapSO _helperPlayerAnimationMap;

    private PlayerTerrainAbility _terrainAbility;
    private PlayerAnimationAbility _animationAbility;
    private HelperController _currentHelper;
    private HelperController _backHelper;

    public HelperController CurrentHelper => _currentHelper;
    public HelperController BackHelper => _backHelper;
    public Transform EquipSlot => _equipSlot;
    public Transform BackEquipSlot => _backEquipSlot;

    protected override void Awake()
    {
        base.Awake();
        _terrainAbility = _owner.GetAbility<PlayerTerrainAbility>();
        _animationAbility = _owner.GetAbility<PlayerAnimationAbility>();
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

        _currentHelper.DetachForDespawn();

        HelperController helperToDestroy = _currentHelper;
        _currentHelper = null;

        PlayDespawnAndDestroy(helperToDestroy);
    }

    public void UnsummonBack()
    {
        if (_backHelper == null) return;

        _backHelper.DetachForDespawn();

        HelperController helperToDestroy = _backHelper;
        _backHelper = null;

        PlayDespawnAndDestroy(helperToDestroy);
    }

    private void PlayDespawnAndDestroy(HelperController helper)
    {
        if (helper == null) return;

        helper.BeginDespawn();

        helper.PhotonView.RpcSafe(
            nameof(HelperController.RPC_PlayDespawnShrink),
            RpcTarget.Others);

        HelperSummonVfxAbility vfx = helper.GetAbility<HelperSummonVfxAbility>();
        if (vfx == null)
        {
            DestroyHelper(helper);
            return;
        }

        vfx.PlayOnDespawn(() => DestroyHelper(helper));
    }

    private void DestroyHelper(HelperController helper)
    {
        if (helper == null) return;

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Destroy(helper.gameObject);
        else
            Destroy(helper.gameObject);
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
                _currentHelper.Equip(_equipSlot, false);
                _currentHelper.PhotonView.RpcSafe(
                    nameof(HelperController.RPC_Equip), RpcTarget.Others, _owner.PhotonView.ViewID, false);
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
                _backHelper.Equip(slot, true);
                _backHelper.PhotonView.RpcSafe(
                    nameof(HelperController.RPC_Equip), RpcTarget.Others, _owner.PhotonView.ViewID, true);
            }
        }
    }

    private void TryInteractPrimary()
    {
        TerrainCell cell = GetTargetCell(out bool isBelowFallback);
        if (cell == null) return;
        if (isBelowFallback) return;

        if (_currentHelper.InteractPrimary(cell))
            PlayHelperPlayerTrigger(_currentHelper?.Data, isPrimary: true);
    }

    private bool IsGroundHelper(HelperController helper)
    {
        return helper != null && helper.GetAbility<GroundActionAbility>() != null;
    }

    private void TryInteractSecondary()
    {
        TerrainCell cell = GetTargetCell(out _);
        if (cell == null) return;

        if (_currentHelper.InteractSecondary(cell))
            PlayHelperPlayerTrigger(_currentHelper?.Data, isPrimary: false);
    }

    private TerrainCell GetTargetCell(out bool isBelowFallback)
    {
        isBelowFallback = false;

        if (IsGroundHelper(_currentHelper))
            return _terrainAbility.GetFrontCellForGroundHelper();

        return _terrainAbility.GetFrontCell(out isBelowFallback);
    }

    private void PlayHelperPlayerTrigger(HelperDataSO helperData, bool isPrimary)
    {
        if (_helperPlayerAnimationMap == null || helperData == null) return;

        if (!_helperPlayerAnimationMap.TryGetTriggers(
            helperData,
            out string primaryTrigger,
            out string secondaryTrigger))
            return;

        string triggerName = isPrimary ? primaryTrigger : secondaryTrigger;
        if (string.IsNullOrWhiteSpace(triggerName)) return;
        _animationAbility?.PlayTriggerSynced(triggerName);
    }
}
