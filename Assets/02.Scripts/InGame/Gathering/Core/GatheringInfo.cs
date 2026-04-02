using Photon.Realtime;

public readonly struct GatheringInfo
{
    private readonly HelperController _helperController;

    public GatheringInfo(HelperController helperController)
    {
        _helperController = helperController;
    }

    public PlayerController Player => _helperController.PlayerOwner;
    public int Damage => _helperController.Data.GatherDamage;
    public HelperGrade HelperGrade => _helperController.Grade;
    public HelperExperience HelperExperience => _helperController.Experience;

}