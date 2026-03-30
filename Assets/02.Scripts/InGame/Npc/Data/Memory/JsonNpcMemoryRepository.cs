using UnityEngine;
using System.IO;
using Cysharp.Threading.Tasks;

// 로컬 npc 기억 저장소입니다.
public class JsonNpcMemoryRepository : INpcMemoryRepository
{
    private readonly string _rootPath;

    public JsonNpcMemoryRepository()
    {
        _rootPath = Path.Combine(Application.persistentDataPath, "NpcMemory");
        if (!Directory.Exists(_rootPath))
        {
            Directory.CreateDirectory(_rootPath);
        }
    }

    public async UniTask<NpcMemoryProfile> LoadMemoryAsync(string npcId, string playerId)
    {
        string path = GetPath(npcId, playerId);

        if (!File.Exists(path))
        {
            return new NpcMemoryProfile
            {
                NpcId = npcId,
                PlayerId = playerId,
                Friendship = 0,
                FriendshipStep = "Awkward"
            };
        }

        string json = await File.ReadAllTextAsync(path);
        return JsonUtility.FromJson<NpcMemoryProfile>(json);
    }

    public async UniTask SaveMemoryAsync(NpcMemoryProfile profile)
    {
        string path = GetPath(profile.NpcId, profile.PlayerId);
        string json = JsonUtility.ToJson(profile, true);
        await File.WriteAllTextAsync(path, json);
    }

    private string GetPath(string npcId, string playerId)
    {
        return Path.Combine(_rootPath, $"{npcId}_{playerId}.json");
    }
}
