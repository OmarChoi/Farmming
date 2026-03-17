using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerStatSO _statSo;

    public PlayerStatSO StatSo => _statSo;
    public string PlayerId { get; private set; }
    public bool IsUIOpen { get; private set; }

    private readonly Dictionary<Type, PlayerAbility> _abilityCache = new();

    private void Start()
    {
        // TODO: PUN2 도입 후 PhotonView.Owner.ActorNumber.ToString()으로 변경
        PlayerId = "local";

        if (SaveManager.Instance != null)
            SaveManager.Instance.RegisterPlayer(PlayerId, this);
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
    }

    public void ExitUIMode()
    {
        IsUIOpen = false;
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

        GetAbility<PlayerInventoryAbility>()?.ExportTo(saveData);

        return saveData;
    }

    public void ImportSaveData(PlayerSaveData saveData, ItemDatabase itemDb)
    {
        transform.position = new Vector3(saveData.PosX, saveData.PosY, saveData.PosZ);
        transform.rotation = Quaternion.Euler(0f, saveData.RotY, 0f);

        GetAbility<PlayerInventoryAbility>()?.ImportFrom(saveData, itemDb);
    }
}