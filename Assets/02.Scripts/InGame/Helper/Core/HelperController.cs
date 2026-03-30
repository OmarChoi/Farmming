using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class HelperController : MonoBehaviour
{
    [SerializeField] private HelperDataSO _data;

    public HelperDataSO Data => _data;
    public string HelperId => _data.HelperId;
    public PlayerController PlayerOwner { get; private set; }
    public EHelperState State { get; private set; } = EHelperState.Inventory;
    public Transform FollowTarget { get; private set; }

    public bool IsActing { get; private set; }

    public event Action OnActionStarted;
    public event Action OnActionEnded;

    public HelperLevel Level { get; private set; }
    public HelperGrade Grade { get; private set; }
    public HelperEnergy Energy { get; private set; }

    public PhotonView PhotonView { get; private set; }
    public bool IsMine => PhotonView == null || PhotonView.IsMine;

    private readonly Dictionary<Type, HelperAbility> _abilityCache = new();
    private PhotonTransformView _transformView;

    private const float SummonOffset = 1.5f;

    private void Awake()
    {
        PhotonView = GetComponent<PhotonView>();
        _transformView = GetComponent<PhotonTransformView>();

        Level = new HelperLevel(_data);
        Grade = new HelperGrade(_data);
        Energy = new HelperEnergy(_data);

        Energy.OnExhausted += OnEnergyExhausted;
        Energy.OnRecovered += OnEnergyRecovered;
    }

    private void OnDestroy()
    {
        Energy.OnExhausted -= OnEnergyExhausted;
        Energy.OnRecovered -= OnEnergyRecovered;
    }

    private void OnEnergyExhausted()
    {
        Debug.Log("에너지 소진");
    }

    private void OnEnergyRecovered()
    {
        Debug.Log("에너지 회복");
    }

    private void Update()
    {
        if (!IsMine) return;
        Energy.Recover(Time.deltaTime);
    }

    public T GetAbility<T>() where T : HelperAbility
    {
        var type = typeof(T);

        if (_abilityCache.TryGetValue(type, out var cached))
            return cached as T;

        var ability = GetComponentInChildren<T>();
        if (ability != null)
            _abilityCache[type] = ability;

        return ability;
    }

    public void Summon(PlayerController playerOwner)
    {
        PlayerOwner = playerOwner;
        FollowTarget = playerOwner.transform;
        State = EHelperState.Summoned;
        transform.SetParent(null);
        transform.position = FollowTarget.position + FollowTarget.right * SummonOffset;
        gameObject.SetActive(true);
        GetAbility<HelperInteractionAbility>()?.Init();
    }

    public void Equip(Transform equipSlot)
    {
        SetTransformSync(false);
        State = EHelperState.Equipped;
        transform.SetParent(equipSlot);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        GetAbility<HelperAnimationAbility>()?.Play(EHelperAnim.Equipped);
    }

    public void Unequip()
    {
        State = EHelperState.Summoned;
        transform.SetParent(null);
        transform.position = FollowTarget.position;
        SetTransformSync(true);

        Vector3 backDir = -FollowTarget.forward;
        GetAbility<HelperFollowAbility>()?.LaunchBack(backDir);
    }

    private void SetTransformSync(bool enabled)
    {
        if (_transformView != null)
            _transformView.enabled = enabled;
    }

    public void LoadState(HelperSaveData data)
    {
        Level.CurrentLevel = data.Level;
        Grade.CurrentGrade = (EHelperGrade)data.Grade;
    }

    public void BeginAction()
    {
        if (IsActing) return;
        IsActing = true;
        OnActionStarted?.Invoke();
    }

    public void EndAction()
    {
        if (!IsActing) return;
        IsActing = false;
        OnActionEnded?.Invoke();
    }

    public void InteractPrimary(TerrainCell cell)
    {
        GetAbility<HelperInteractionAbility>()?.InteractPrimary(cell);
    }

    public void InteractSecondary(TerrainCell cell)
    {
        GetAbility<HelperInteractionAbility>()?.InteractSecondary(cell);
    }

    #region PUN2 RPC — 원격 상태 동기화 수신부

    [PunRPC]
    internal void RPC_Summon(int ownerViewId)
    {
        var ownerView = PhotonView.Find(ownerViewId);
        if (ownerView == null) return;

        var playerController = ownerView.GetComponent<PlayerController>();
        if (playerController == null) return;

        PlayerOwner = playerController;
        FollowTarget = playerController.transform;
        State = EHelperState.Summoned;
        transform.SetParent(null);
        gameObject.SetActive(true);
    }

    [PunRPC]
    internal void RPC_Equip(int ownerViewId)
    {
        var ownerView = PhotonView.Find(ownerViewId);
        if (ownerView == null) return;

        var equipSlot = ownerView.GetComponentInChildren<PlayerHelperInteractionAbility>()?.EquipSlot;
        if (equipSlot == null) return;

        SetTransformSync(false);
        State = EHelperState.Equipped;
        transform.SetParent(equipSlot);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    [PunRPC]
    internal void RPC_Unequip()
    {
        State = EHelperState.Summoned;
        transform.SetParent(null);
        SetTransformSync(true);
    }

    [PunRPC]
    internal void RPC_PlayAnimation(int anim)
    {
        GetAbility<HelperAnimationAbility>()?.PlayLocal((EHelperAnim)anim);
    }

    [PunRPC]
    internal void RPC_InteractPrimary(int gridX, int gridY, int gridZ)
    {
        var cell = TerrainGridManager.Instance?.GetCell(new Vector3Int(gridX, gridY, gridZ));
        if (cell == null) return;

        GetAbility<HelperInteractionAbility>()?.InteractPrimaryLocal(cell);
    }

    [PunRPC]
    internal void RPC_InteractSecondary(int gridX, int gridY, int gridZ)
    {
        var cell = TerrainGridManager.Instance?.GetCell(new Vector3Int(gridX, gridY, gridZ));
        if (cell == null) return;

        GetAbility<HelperInteractionAbility>()?.InteractSecondaryLocal(cell);
    }

    #endregion
}