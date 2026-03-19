public readonly struct GatheringInfo
{
    public readonly PlayerController Player;
    public readonly int Damage;
    public readonly HelperGrade HelperGrade;

    public GatheringInfo(HelperController helperController)
    {
        Player = helperController.PlayerOwner;
        Damage = helperController.Data.GatherDamage;
        HelperGrade = helperController.Grade;
    }
}