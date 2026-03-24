using UnityEngine;

public class PlayerCustomizeAbility : PlayerAbility, ISaveableAbility
{
    [SerializeField] private CharacterPartSwapper _partSwapper;
    [SerializeField] private CustomizeData _customizeData;

    private void Start()
    {
        if (_customizeData != null && _partSwapper != null)
            _customizeData.ApplyTo(_partSwapper);
    }

    public void ExportTo(PlayerSaveData saveData)
    {
        saveData.Customize = new CustomizeSaveData
        {
            BodyIndex = _partSwapper.currentBodyIndex,
            HairIndex = _partSwapper.currentHairIndex,
            HatIndex = _partSwapper.currentHatIndex,
            EyeIndex = _partSwapper.currentEyeIndex,
            MouthIndex = _partSwapper.currentMouthIndex,
            EyebrowIndex = _partSwapper.currentEyebrowIndex,
            CheekIndex = _partSwapper.currentCheekIndex
        };
    }

    public void ImportFrom(PlayerSaveData saveData)
    {
        if (saveData.Customize == null) return;

        _partSwapper.SetBody(saveData.Customize.BodyIndex);
        _partSwapper.SetHair(saveData.Customize.HairIndex);
        _partSwapper.SetHat(saveData.Customize.HatIndex);
        _partSwapper.SetEye(saveData.Customize.EyeIndex);
        _partSwapper.SetMouth(saveData.Customize.MouthIndex);
        _partSwapper.SetEyebrow(saveData.Customize.EyebrowIndex);
        _partSwapper.SetCheek(saveData.Customize.CheekIndex);
    }
}