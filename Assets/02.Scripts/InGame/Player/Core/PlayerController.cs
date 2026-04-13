using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerStatSO _statSo;

    public PlayerStatSO StatSo => _statSo;

    public PhotonView PhotonView { get; private set; }
    public bool IsMine => PhotonView == null || !PhotonNetwork.IsConnected || PhotonView.IsMine;
    public string PlayerId { get; private set; }
    public bool IsUIOpen { get; private set; }
    public bool CanMove => !IsUIOpen && !IsActionLocked;
    public bool CanRotateCamera => !IsUIOpen && (!IsActionLocked || AllowCameraRotation);
    public bool IsActionLocked { get; private set; }
    public bool AllowCameraRotation { get; set; }

    public void LockAction() => IsActionLocked = true;
    public void UnlockAction() => IsActionLocked = false;

    private readonly Dictionary<Type, PlayerAbility> _abilityCache = new();
    private Renderer[] _cachedRenderers;


    private void Awake()
    {
        PhotonView = GetComponent<PhotonView>();
    }

    private void Start()
    {
        // IsMine → 로컬 GUID, 원격 → NickName (상대방의 GUID)
        if (PhotonView != null && !PhotonView.IsMine && PhotonView.Owner != null)
            PlayerId = PhotonView.Owner.NickName;
        else
            PlayerId = NetworkManager.Instance != null
                ? NetworkManager.Instance.GetPlayerId()
                : "local";

        // 마스터: 모든 플레이어 등록 (저장 대상)
        if (Photon.Pun.PhotonNetwork.IsMasterClient && SaveManager.Instance != null)
            SaveManager.Instance.RegisterPlayer(PlayerId, this);

        if (!IsMine) return;

        // 로컬 전용: 자기 자신도 등록 (비마스터 클라이언트)
        if (!Photon.Pun.PhotonNetwork.IsMasterClient && SaveManager.Instance != null)
            SaveManager.Instance.RegisterPlayer(PlayerId, this);

        SetCursorLock(true);
    }

    private void Update()
    {
        if (!IsMine) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            SetCursorLock(Cursor.lockState != CursorLockMode.Locked);
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null && PlayerId != null)
            SaveManager.Instance.UnregisterPlayer(PlayerId);
    }

    public T GetAbility<T>() where T : PlayerAbility
    {
        var type = typeof(T);

        if (_abilityCache.TryGetValue(type, out var cached))
            return cached as T;

        var ability = GetComponentInChildren<T>();
        if (ability != null)
            _abilityCache[type] = ability;

        return ability;
    }

    public void EnterUIMode()
    {
        IsUIOpen = true;
        SetCursorLock(false);
    }

    public void ExitUIMode()
    {
        IsUIOpen = false;
        SetCursorLock(true);
    }

    public void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    public void SetVisualsVisible(bool visible)
    {
        _cachedRenderers ??= GetComponentsInChildren<Renderer>(true);

        foreach (var renderer in _cachedRenderers)
        {
            if (renderer == null)
                continue;

            renderer.enabled = visible;
        }
    }

    public PlayerSaveData ExportSaveData(string playerId)
    {
        var saveData = new PlayerSaveData
        {
            PlayerId = playerId,
            PosX = transform.position.x,
            PosY = transform.position.y,
            PosZ = transform.position.z,
            RotY = transform.eulerAngles.y
        };

        foreach (var saveable in GetComponentsInChildren<ISaveableAbility>())
            saveable.ExportTo(saveData);

        return saveData;
    }

    public void ImportSaveData(PlayerSaveData saveData)
    {
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        transform.position = new Vector3(saveData.PosX, saveData.PosY, saveData.PosZ);
        transform.rotation = Quaternion.Euler(0f, saveData.RotY, 0f);

        if (cc != null) cc.enabled = true;

        foreach (var saveable in GetComponentsInChildren<ISaveableAbility>())
            saveable.ImportFrom(saveData);
    }

    // === PunRPC ===

    [PunRPC]
    public void RPC_AnimTrigger(string triggerName)
    {
        GetAbility<PlayerAnimationAbility>()?.PlayTrigger(triggerName);
    }

    [PunRPC]
    public void RPC_RestoreSaveData(string json)
    {
        var saveData = JsonUtility.FromJson<PlayerSaveData>(json);
        ImportSaveData(saveData);
    }

    [PunRPC]
    public void RPC_SyncCustomize(string json)
    {
        GetAbility<PlayerCustomizeAbility>()?.ApplyFromJson(json);
    }

    [PunRPC]
    public void RPC_RequestSaveData()
    {
        var saveData = ExportSaveData(PlayerId);
        string json = JsonUtility.ToJson(saveData);
        PhotonView.RPC(nameof(RPC_RespondSaveData), RpcTarget.MasterClient, json);
    }

    [PunRPC]
    public void RPC_RespondSaveData(string json)
    {
        var saveData = JsonUtility.FromJson<PlayerSaveData>(json);
        if (SaveManager.Instance != null)
            SaveManager.Instance.ReceiveSaveData(saveData);
    }

    [PunRPC]
    public void RPC_ApplyTrouble(int effectType, Vector3 direction, float power, float duration)
    {
        TroubleContext context = new TroubleContext
        {
            Source = null,
            EffectType = (ETroubleEffectType)effectType,
            Direction = direction,
            Power = power,
            Duration = duration
        };

        GetAbility<PlayerTroubleAbility>()?.ApplyTrouble(context);
    }
}
