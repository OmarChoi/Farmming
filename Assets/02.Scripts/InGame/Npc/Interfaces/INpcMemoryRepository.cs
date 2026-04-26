using Cysharp.Threading.Tasks;

public interface INpcMemoryRepository
{
    UniTask<NpcMemoryProfile> LoadMemoryAsync(string npcId, string playerId);
    UniTask SaveMemoryAsync(NpcMemoryProfile profile);

    bool DeleteMemory(string npcId, string playerId);
}
