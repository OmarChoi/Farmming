using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public interface INpcMemoryRagService
{
    UniTask InitializeMemoryRagAsync();
    UniTask RebuildIndexMemoryRagAsync(NpcMemoryProfile profile);
    UniTask<List<string>> SearchRelevantMemoriesAsync(string npcId, string playerId, string query, int topK = 4);
}
