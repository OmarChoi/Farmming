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

        foreach (var kvp in _players)
            data.Players.Add(kvp.Value.ExportSaveData(kvp.Key));

        await _repository.SaveAsync(data, slot);
        Debug.Log($"저장 완료 (슬롯 {slot})");
    }

    public async UniTask LoadAsync(int slot = 0)
    {
        SaveData data = await _repository.LoadAsync(slot);
        if (data == null)
        {
            Debug.Log($"저장 데이터 없음 (슬롯 {slot})");
            return;
        }

        _terrainGridManager.ImportSaveData(data.Terrain);

        foreach (var playerSave in data.Players)
        {
            if (_players.TryGetValue(playerSave.PlayerId, out var target))
                target.ImportSaveData(playerSave);
        }

        Debug.Log($"로드 완료 (슬롯 {slot})");
    }

    public UniTask<bool> HasSaveAsync(int slot = 0) => _repository.HasSaveAsync(slot);

    private SaveData _loadedData;

    public async UniTask PreloadAsync(int slot = 0)
    {
        _loadedData = await _repository.LoadAsync(slot);
    }

    public bool HasPlayerData(string playerId)
    {
        if (_loadedData == null) return false;
        return _loadedData.Players.Exists(p => p.PlayerId == playerId);
    }
}