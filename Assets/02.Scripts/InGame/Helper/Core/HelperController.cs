using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class HelperController : MonoBehaviour
{
    [SerializeField] private HelperDataSO _data;
    [SerializeField] private Vector3 _rotationEuler = new Vector3(0f, 180f, 0f);

    public HelperDataSO Data => _data;
    public string HelperId => _data.HelperId;
    public PlayerController PlayerOwner { get; private set; }
    public EHelperState State { get; private set; } = EHelperState.Inventory;
    public Transform FollowTarget { get; private set; }

    public bool IsActing { get; private set; }

    public event Action OnActionStarted;
    public event Action OnActionEnded;
    public event Action OnGradeChanged;

    public HelperLevel Level { get; private set; }
    public HelperGrade Grade { get; private set; }
    public HelperEnergy Energy { get; private set; }
    public HelperExperience Experience { get; private set; }

    public PhotonView PhotonView { get; private set; }
    public bool IsMine => PhotonView == null || !PhotonNetwork.IsConnected || PhotonView.IsMine;

    private readonly Dictionary<Type, HelperAbility> _abilityCache = new();
    private PhotonTransformView _transformView;

    private const float SummonOffset = 1.5f;

    private Vector3 _originalScale;
    private float _equippedSmallScale = 0.55f;

    private void Awake()
    {
        PhotonView = GetComponent<PhotonView>();
        _transformView = GetComponent<PhotonTransformView>();

        Level = new HelperLevel(_data);
        Grade = new HelperGrade(_data);
        Energy = new HelperEnergy(_data);
        Experience = new HelperExperience(_data, Grade);

        Energy.OnExhausted += OnEnergyExhausted;
        Energy.OnRecovered += OnEnergyRecovered;

        _originalScale = transform.localScale;
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

        var ability = GetComponent<T>();
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

        IEquipOverride equipOverride = GetComponentInChildren<IEquipOverride>();
        if (equipOverride != null)
        {
            transform.localRotation = Quaternion.Euler(equipOverride.GetEquipRotation());
            transform.localScale = _originalScale * equipOverride.GetEquipScale();
        }

        GetAbility<HelperAnimationAbility>()?.Play(EHelperAnim.Equipped);
    }

    public void Unequip()
    {
        State = EHelperState.Summoned;
        transform.SetParent(null);
        transform.localScale = _originalScale;
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
        Experience.Load(data.Experience);

        float elapsed = data.EnergySavedAt > 0
            ? (float)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - data.EnergySavedAt)
            : 0f;
        Energy.Load(data.Energy, elapsed);
    }

    public void PerformUpgrade()
    {
        if(!Experience.IsReadyToUpgrade)
        {
            return;
        }
        Grade.Upgrade();
        Experience.Reset();
        OnGradeChanged?.Invoke();
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

    public bool InteractPrimary(TerrainCell cell)
    {
        return GetAbility<HelperInteractionAbility>()?.InteractPrimary(cell) ?? false;
    }

    public bool InteractSecondary(TerrainCell cell)
    {
        return GetAbility<HelperInteractionAbility>()?.InteractSecondary(cell) ?? false;
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
        GetAbility<HelperInteractionAbility>()?.Init();
    }

    [PunRPC]
    internal void RPC_Equip(int ownerViewId, bool isBack)
    {
        var ownerView = PhotonView.Find(ownerViewId);
        if (ownerView == null) return;

        var interaction = ownerView.GetComponentInChildren<PlayerHelperInteractionAbility>();
        if (interaction == null) return;

        var equipSlot = (isBack && interaction.BackEquipSlot != null) ? interaction.BackEquipSlot : interaction.EquipSlot;
        if (equipSlot == null) return;

        Equip(equipSlot);
    }

    [PunRPC]
    internal void RPC_Unequip()
    {
        State = EHelperState.Summoned;
        transform.SetParent(null);
        SetTransformSync(true);
    }

    #endregion
}
