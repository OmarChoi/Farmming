using System;
using UnityEngine;

public class PlayerCustomizeAbility : PlayerAbility, ISaveableAbility
{
    [SerializeField] private CharacterPartSwapper _partSwapper;

    public static event Action<PlayerCustomizeAbility> OnReady;

    private void Start()
    {
        OnReady?.Invoke(this);
    }

    public void Initialize(CustomizeSaveData data)
    {
        if (data != null && _partSwapper != null)
            _partSwapper.ApplySaveData(data);
    }

    public void ExportTo(PlayerSaveData saveData)
    {
        if (_partSwapper != null)
            saveData.Customize = _partSwapper.CreateSaveData();
    }

    public void ImportFrom(PlayerSaveData saveData)
    {
        if (saveData.Customize == null) return;

        _partSwapper.ApplySaveData(saveData.Customize);
    }
}