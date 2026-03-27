using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private TerrainGridManager _terrainGridManager;
    [SerializeField] private MapManager _mapManager;

    private readonly Dictionary<string, PlayerController> _players = new();
    private ISaveRepository _repository;
    private SaveData _loadedData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _repository = new LocalJsonSaveRepository();
    }

    public void RegisterPlayer(string playerId, PlayerController player)
    {
        Debug.Log($"[SaveManager] RegisterPlayer: id={playerId}, isMine={player.PhotonView?.IsMine}");
        _players[playerId] = player;
    }

    public void UnregisterPlayer(string playerId)
    {
        _players.Remove(playerId);
    }

    public async UniTask SaveAsync(int slot = 0)
    {
        var data = new SaveData();

        if (_mapManager == null || _mapManager.IsVillage)
            data.Terrain = _terrainGridManager.ExportSaveData();

        Debug.Log($"[SaveManager] 등록된 플레이어 수: {_players.Count}");
        foreach (var kvp in _players)
        {
            Debug.Log($"[SaveManager] 저장 중: {kvp.Key} (pos={kvp.Value.transform.position})");
            data.Players.Add(kvp.Value.ExportSaveData(kvp.Key));
        }

        await _repository.SaveAsync(data, slot);
        Debug.Log($"저장 완료 (슬롯 {slot}, 플레이어 {data.Players.Count}명)");
    }

    public async UniTask LoadAsync(int slot = 0)
    {
        _loadedData = await _repository.LoadAsync(slot);
        if (_loadedData == null)
        {
            Debug.Log($"저장 데이터 없음 (슬롯 {slot})");
            return;
        }

        _terrainGridManager.ImportSaveData(_loadedData.Terrain);
        Debug.Log($"로드 완료 (슬롯 {slot}, 플레이어 데이터 {_loadedData.Players.Count}명)");
    }

    /// 등록된 플레이어에게 로드된 세이브 데이터 적용
    public UniTask ApplyLoadedPlayers()
    {
        if (_loadedData == null) return UniTask.CompletedTask;

        foreach (var playerSave in _loadedData.Players)
        {
            if (_players.TryGetValue(playerSave.PlayerId, out var target))
            {
                Debug.Log($"[SaveManager] 플레이어 복원: {playerSave.PlayerId}");
                target.ImportSaveData(playerSave);
            }
        }

        return UniTask.CompletedTask;
    }

    public UniTask<bool> HasSaveAsync(int slot = 0) => _repository.HasSaveAsync(slot);

    public bool HasPlayerData(string playerId)
    {
        if (_loadedData == null) return false;
        return _loadedData.Players.Exists(p => p.PlayerId == playerId);
    }
}