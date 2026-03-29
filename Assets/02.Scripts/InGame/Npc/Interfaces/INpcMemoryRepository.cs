using Cysharp.Threading.Tasks;

public interface INpcMemoryRepository
{
    UniTask<NpcMemoryProfile> LoadMemoryAsync(string npcId, string playerId);
    UniTask SaveMemoryAsync(NpcMemoryProfile profile);
}
