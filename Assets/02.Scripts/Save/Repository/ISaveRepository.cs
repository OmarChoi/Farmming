public interface ISaveRepository
{
    void Save(SaveData data, int slot);
    SaveData Load(int slot);
    bool HasSave(int slot);
    void Delete(int slot);
}