public interface ISaveableAbility
{
    void ExportTo(PlayerSaveData saveData);
    void ImportFrom(PlayerSaveData saveData);
}