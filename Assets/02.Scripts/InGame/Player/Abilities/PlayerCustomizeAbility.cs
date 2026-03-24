using UnityEngine;

public class PlayerCustomizeAbility : PlayerAbility, ISaveableAbility
{
    [SerializeField] private CharacterPartSwapper _partSwapper;

    private void Start()
    {
        if (CustomizeData.Instance != null && _partSwapper != null)
            CustomizeData.Instance.ApplyTo(_partSwapper);
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