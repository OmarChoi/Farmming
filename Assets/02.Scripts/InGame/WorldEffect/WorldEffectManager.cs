using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class WorldEffectManager : MonoBehaviour
{
    public static WorldEffectManager Instance { get; private set; }

    private const int PermanentDuration = -1;

    [SerializeField] private WorldEffectDatabase _database;

    private readonly List<WorldEffectEntry> _activeEffects = new();
    private WorldEffectNetworkSync _sync;

    public IReadOnlyList<WorldEffectEntry> ActiveEffects => _activeEffects;
    public WorldEffectDatabase Database => _database;

    private bool HasAuthority => !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
    private bool CanUseNetworkSync => PhotonNetwork.IsConnected && PhotonNetwork.InRoom && _sync != null;
    
    public static int ApplyMultiplier(int baseValue, float multiplier)
    {
        return multiplier switch
        {
            > 1 => Mathf.Max(0, Mathf.CeilToInt(baseValue * multiplier)),
            < 1 => Mathf.Max(0, Mathf.FloorToInt(baseValue * multiplier)),
            _   => baseValue
        };
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        TimeEvents.OnNetDayStarted += OnDayStarted;
    }

    private void OnDisable()
    {
        TimeEvents.OnNetDayStarted -= OnDayStarted;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RegisterSync(WorldEffectNetworkSync sync)
    {
        _sync = sync;
        if (HasAuthority && CanUseNetworkSync)
        {
            _sync.BroadcastSnapshot(ExportSaveData());
        }
    }

    public void UnregisterSync(WorldEffectNetworkSync sync)
    {
        if (_sync == sync) _sync = null;
    }

    public void AddEffect(string effectId, EWorldEffectKind kind, int durationDays)
    {
        if (!IsValidRequest(effectId, durationDays)) return;

        if (!HasAuthority)
        {
            if (CanUseNetworkSync)
            {
                _sync.RequestAddEffect(effectId, (byte)kind, durationDays);
            }
            return;
        }

        ExecuteAddEffect(effectId, kind, durationDays);
        BroadcastSnapshotIfConnected();
    }

    public void RemoveEffect(string effectId)
    {
        if (string.IsNullOrWhiteSpace(effectId)) return;

        if (!HasAuthority)
        {
            if (CanUseNetworkSync)
                _sync.RequestRemoveEffect(effectId);
            return;
        }

        ExecuteRemoveEffect(effectId);
        BroadcastSnapshotIfConnected();
    }

    public bool HasEffect(string effectId)
    {
        if (string.IsNullOrWhiteSpace(effectId)) return false;

        foreach (WorldEffectEntry effectEntry in _activeEffects)
        {
            if (effectEntry.EffectId == effectId) return true;
        }

        return false;
    }

    public float GetAcquireMultiplier() => ComputeMultiplier(EWorldEffectCategory.Acquire);
    public float GetSellPriceMultiplier() => ComputeMultiplier(EWorldEffectCategory.SellPrice);

    private float ComputeMultiplier(EWorldEffectCategory category)
    {
        if (_database == null) return 1f;

        var sum = 0f;
        foreach (WorldEffectEntry effectEntry in _activeEffects)
        {
            WorldEffectDataSO data = _database.GetById(effectEntry.EffectId);
            if (data == null || data.Category != category) continue;
            sum += data.PercentModifier;
        }

        return Mathf.Max(0f, 1f + sum);
    }

    public WorldEffectSaveData ExportSaveData()
    {
        var data = new WorldEffectSaveData();
        for (int i = 0; i < _activeEffects.Count; i++)
        {
            data.Effects.Add(CloneEntry(_activeEffects[i]));
        }

        return data;
    }

    public void ImportSaveData(WorldEffectSaveData saveData)
    {
        ClearEffects();

        if (saveData?.Effects == null)
        {
            WorldEffectEvents.InvokeEffectsChanged();
            return;
        }

        for (int i = 0; i < saveData.Effects.Count; i++)
        {
            WorldEffectEntry entry = saveData.Effects[i];
            if (entry == null || !IsValidRequest(entry.EffectId, entry.TotalDurationDays)) continue;

            _activeEffects.Add(CloneEntry(entry));
            WorldEffectEvents.InvokeEffectAdded(_activeEffects[^1]);
        }

        WorldEffectEvents.InvokeEffectsChanged();
    }

    public void ClearEffects()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            WorldEffectEvents.InvokeEffectRemoved(_activeEffects[i]);
        }

        _activeEffects.Clear();
        WorldEffectEvents.InvokeEffectsChanged();
    }

    public void ExecuteAddEffect(string effectId, EWorldEffectKind kind, int durationDays)
    {
        if (!IsValidRequest(effectId, durationDays)) return;

        foreach (WorldEffectEntry effectEntry in _activeEffects)
        {
            if (effectEntry.EffectId != effectId) continue;

            effectEntry.Kind = (byte)kind;
            effectEntry.TotalDurationDays = durationDays;
            effectEntry.RemainingDays = durationDays;
            WorldEffectEvents.InvokeEffectsChanged();
            return;
        }

        var entry = new WorldEffectEntry
        {
            EffectId = effectId,
            Kind = (byte)kind,
            TotalDurationDays = durationDays,
            RemainingDays = durationDays
        };

        _activeEffects.Add(entry);
        WorldEffectEvents.InvokeEffectAdded(entry);
        WorldEffectEvents.InvokeEffectsChanged();
    }

    public void ExecuteRemoveEffect(string effectId)
    {
        if (string.IsNullOrWhiteSpace(effectId)) return;

        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            if (_activeEffects[i].EffectId != effectId) continue;

            WorldEffectEntry removed = _activeEffects[i];
            _activeEffects.RemoveAt(i);
            WorldEffectEvents.InvokeEffectRemoved(removed);
            WorldEffectEvents.InvokeEffectsChanged();
            return;
        }
    }

    internal void ApplySnapshot(WorldEffectSaveData saveData)
    {
        ImportSaveData(saveData);
    }

    private void OnDayStarted()
    {
        if (!HasAuthority) return;

        bool changed = false;
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            WorldEffectEntry entry = _activeEffects[i];
            if (entry.RemainingDays == PermanentDuration) continue;

            entry.RemainingDays--;
            if (entry.RemainingDays <= 0)
            {
                _activeEffects.RemoveAt(i);
                WorldEffectEvents.InvokeEffectRemoved(entry);
            }

            changed = true;
        }

        if (!changed) return;

        WorldEffectEvents.InvokeEffectsChanged();
        BroadcastSnapshotIfConnected();
    }

    private void BroadcastSnapshotIfConnected()
    {
        if (CanUseNetworkSync)
        {
            _sync.BroadcastSnapshot(ExportSaveData());
        }
    }

    private static bool IsValidRequest(string effectId, int durationDays)
    {
        return !string.IsNullOrWhiteSpace(effectId) &&
               (durationDays == PermanentDuration || durationDays > 0);
    }

    private static WorldEffectEntry CloneEntry(WorldEffectEntry source)
    {
        return new WorldEffectEntry
        {
            EffectId = source.EffectId,
            Kind = source.Kind,
            TotalDurationDays = source.TotalDurationDays,
            RemainingDays = source.RemainingDays
        };
    }
}
