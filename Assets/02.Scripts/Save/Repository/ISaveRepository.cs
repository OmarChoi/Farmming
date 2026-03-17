using Cysharp.Threading.Tasks;

public interface ISaveRepository
{
    UniTask SaveAsync(SaveData data, int slot);
    UniTask<SaveData> LoadAsync(int slot);
    UniTask<bool> HasSaveAsync(int slot);
    UniTask DeleteAsync(int slot);
}