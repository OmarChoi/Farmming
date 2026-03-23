using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerStatSO _statSo;

    public PlayerStatSO StatSo => _statSo;

    public string PlayerId { get; private set; }
    public bool IsUIOpen { get; private set; }
    public bool CanMove => !IsUIOpen && !IsActionLocked;
    public bool CanRotateCamera => !IsUIOpen && (!IsActionLocked || AllowCameraRotation);
    public bool IsActionLocked { get; private set; }
    public bool AllowCameraRotation { get; set; }

    public void LockAction() => IsActionLocked = true;
    public void UnlockAction() => IsActionLocked = false;

    private readonly Dictionary<Type, PlayerAbility> _abilityCache = new();

    private void Start()
    {
        // TODO: PUN2 도입 후 PhotonView.Owner.ActorNumber.ToString()으로 변경
        PlayerId = "local";
        SetCursorLock(true);

        if (SaveManager.Instance != null)
            SaveManager.Instance.RegisterPlayer(PlayerId, this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SetCursorLock(Cursor.lockState != CursorLockMode.Locked);
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
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
}