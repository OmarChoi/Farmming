using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

public class SaveManager : MonoBehaviourPun
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private TerrainGridManager _terrainGridManager;
    [SerializeField] private MapManager _mapManager;

    private readonly Dictionary<string, PlayerController> _players = new();
    private ISaveRepository _repository;
    private SaveData _loadedData;
    private const float RemoteSaveTimeout = 5f;
    private bool _isSaving;

    private bool _isLoadCompleted;
    public bool IsLoadCompleted => _isLoadCompleted;

    private bool _pendingWorldSave;
    private bool _pendingPlayerOnlySave;

    private string _pendingPlayerId;
    private PlayerSaveData _pendingPlayerSave;
    private int _pendingSlot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _repository = new LocalJsonSaveRepository();
        _isLoadCompleted = false;
    }

    public void EnsureBaseLoadedData()
    {
        if (_loadedData != null) return;
        _loadedData = new SaveData();
    }

    public void MarkLoadCompleted()
    {
        _isLoadCompleted = true;
    }

    // 등록만 합니다. 복원은 하지 않습니다.
    public void RegisterPlayerOnly(string playerId, PlayerController player)
    {
        if (string.IsNullOrEmpty(playerId) || player == null) return;
        _players[playerId] = player;
    }

    // 등록과 동시에 복원을 시도합니다.
    public void RegisterPlayer(string playerId, PlayerController player)
    {
        if (string.IsNullOrEmpty(playerId) || player == null) return;

        _players[playerId] = player;
        TryRestorePlayer(playerId, player);

        bool isNewPlayer =
            _isLoadCompleted &&
            (_loadedData == null || !_loadedData.Players.Exists(p => p.PlayerId == playerId));

        if (isNewPlayer && PhotonNetwork.IsMasterClient)
        {
            int slot = RoomManager.Instance != null ? RoomManager.Instance.SelectedSlot : 0;
            SaveAsync(slot).Forget();
        }
    }

    private void TryRestorePlayer(string playerId, PlayerController player)
    {
        PlayerQuestAbility questAbility = player != null ? player.GetAbility<PlayerQuestAbility>() : null;
        if (!_isLoadCompleted || _loadedData == null) return;

        var save = _loadedData.Players.Find(p => p.PlayerId == playerId);
        if (save == null)
        {
            if (player.IsMine && questAbility != null)
            {
                questAbility.InitializeEmptyState();
            }
            return;
        }

        if (player.PhotonView == null || player.PhotonView.IsMine)
        {
            // 로컬 플레이어: 직접 적용
            player.ImportSaveData(save);
        }
        else
        {
            // 원격 플레이어: RPC로 전체 데이터 전송
            string json = JsonUtility.ToJson(save);
            player.PhotonView.RPC(nameof(PlayerController.RPC_RestoreSaveData), player.PhotonView.Owner, json);
        }
    }

    public void UnregisterPlayer(string playerId)
    {
        _players.Remove(playerId);
    }

    private readonly List<PlayerSaveData> _receivedSaveData = new();
    private int _expectedResponses;

    public void ReceiveSaveData(PlayerSaveData data)
    {
        _receivedSaveData.Add(data);
    }

    public async UniTask SaveAsync(int slot = 0)
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("저장은 마스터 클라이언트만 실행할 수 있습니다.");
            return;
        }

        if (_isSaving)
        {
            _pendingWorldSave = true;
            _pendingSlot = slot;
            Debug.LogWarning("이미 저장 중입니다.");
            return;
        }

        _isSaving = true;

        try
        {
            var data = new SaveData();

            if (_mapManager == null || _mapManager.IsVillage)
                data.Terrain = _terrainGridManager.ExportSaveData();

            if (BuildingManager.Instance != null)
                data.Buildings = BuildingManager.Instance.ExportBuildings();

            if (TimeSystem.Instance != null)
                data.Time = TimeSystem.Instance.ExportSaveData();
            
            if (VillageLevelManager.Instance != null)
                data.Village = VillageLevelManager.Instance.ExportSaveData();

            if (WorldEffectManager.Instance != null)
                data.WorldEffects = WorldEffectManager.Instance.ExportSaveData();

            _receivedSaveData.Clear();
            _expectedResponses = 0;

            foreach (var kvp in _players)
            {
                var player = kvp.Value;
                if (player == null) continue;
                if (player.IsMine)
                {
                    data.Players.Add(player.ExportSaveData(kvp.Key));
                }
                else if (player.PhotonView != null)
                {
                    _expectedResponses++;
                    player.PhotonView.RPC(
                        nameof(PlayerController.RPC_RequestSaveData),
                        player.PhotonView.Owner);
                }
            }

            // 원격 플레이어 응답 대기 (최대 5초)
            if (_expectedResponses > 0)
            {
                float timeout = Time.time + RemoteSaveTimeout;
                await UniTask.WaitUntil(() =>
                    _receivedSaveData.Count >= _expectedResponses || Time.time > timeout);
            }

            data.Players.AddRange(_receivedSaveData);
            _receivedSaveData.Clear();

            // 오프라인 플레이어: _loadedData에 있지만 현재 접속 중이 아닌 플레이어 보존
            if (_loadedData != null)
            {
                var onlineIds = new HashSet<string>();
                foreach (var p in data.Players)
                    onlineIds.Add(p.PlayerId);

                foreach (var saved in _loadedData.Players)
                {
                    if (saved == null) continue;
                    if (!onlineIds.Contains(saved.PlayerId))
                        data.Players.Add(saved);
                }
            }

            _loadedData = data;
            await _repository.SaveAsync(data, slot);

            // 방 커스텀 프로퍼티에 방문 플레이어 목록 갱신
            if (RoomManager.Instance != null)
            {
                var ids = new List<string>();
                foreach (var p in data.Players)
                    ids.Add(p.PlayerId);
                RoomManager.Instance.UpdateVisitedPlayers(ids);
            }

            Debug.Log($"저장 완료 (슬롯 {slot}, 플레이어 {data.Players.Count}명)");
        }
        finally
        {
            _isSaving = false;
            await FlushPendingSaves();
        }
    }

    private async UniTask FlushPendingSaves()
    {
        if (_isSaving) return;

        if (_pendingWorldSave)
        {
            _pendingWorldSave = false;
            int slot = _pendingSlot;
            await SaveAsync(slot);
            return;
        }

        if (_pendingPlayerOnlySave)
        {
            _pendingPlayerOnlySave = false;

            string playerId = _pendingPlayerId;
            PlayerSaveData playerSave = _pendingPlayerSave;
            int slot = _pendingSlot;

            _pendingPlayerId = null;
            _pendingPlayerSave = null;

            await SavePlayerOnlyAsync(playerId, playerSave, slot);
        }
    }

    public async UniTask LoadAsync(int slot = 0)
    {
        _isLoadCompleted = false;

        _loadedData = await _repository.LoadAsync(slot);
        if (_loadedData == null)
        {
            Debug.Log($"저장 데이터 없음 (슬롯 {slot})");
            WorldEffectManager.Instance?.ClearEffects();
            _isLoadCompleted = true;
            RestoreRegisteredPlayers();
            return;
        }

        _mapManager.ImportVillageSaveData(_loadedData.Terrain);

        if (VillageLevelManager.Instance != null)
            VillageLevelManager.Instance.ImportSaveData(_loadedData.Village);
        
        if (BuildingManager.Instance != null && _loadedData.Buildings != null)
            await BuildingManager.Instance.ImportBuildings(_loadedData.Buildings);
        
        if (TimeSystem.Instance != null)
            TimeSystem.Instance.ImportTimeSaveData(_loadedData.Time);

        if (WorldEffectManager.Instance != null)
            WorldEffectManager.Instance.ImportSaveData(_loadedData.WorldEffects);

        _isLoadCompleted = true;
        RestoreRegisteredPlayers();

        Debug.Log($"로드 완료 (슬롯 {slot}, 플레이어 데이터 {_loadedData.Players.Count}명)");
    }

    private void RestoreRegisteredPlayers()
    {
        bool hasNewPlayer = false;

        foreach (var kvp in _players)
        {
            string playerId = kvp.Key;
            PlayerController player = kvp.Value;

            if (player == null) continue;

            bool existsInLoadedData =
                _loadedData != null &&
                _loadedData.Players != null &&
                _loadedData.Players.Exists(p => p != null && p.PlayerId == playerId);

            TryRestorePlayer(playerId, player);

            if (!existsInLoadedData)
            {
                hasNewPlayer = true;
            }
        }

        if (hasNewPlayer && PhotonNetwork.IsMasterClient)
        {
            int slot = RoomManager.Instance != null ? RoomManager.Instance.SelectedSlot : 0;
            SaveAsync(slot).Forget();
        }
    }

    public void RequestPlayerOnlySave(int slot = 0)
    {
        PlayerController local = GetLocalPlayer();
        if (local == null) return;

        string playerId = local.PlayerId;
        PlayerSaveData saveData = local.ExportSaveData(playerId);
        string json = JsonUtility.ToJson(saveData);

        if (PhotonNetwork.IsMasterClient)
        {
            SavePlayerOnlyAsync(playerId, saveData, slot).Forget();
        }
        else
        {
            photonView.RPC(
                nameof(RPC_RequestPlayerOnlySave),
                RpcTarget.MasterClient,
                playerId,
                json,
                slot);
        }
    }

    public async UniTask SavePlayerOnlyAsync(string playerId, PlayerSaveData playerSave, int slot = 0)
    {
        if (string.IsNullOrEmpty(playerId) || playerSave == null) return;

        if (_isSaving)
        {
            _pendingPlayerOnlySave = true;
            _pendingPlayerId = playerId;
            _pendingPlayerSave = playerSave;
            _pendingSlot = slot;
            return;
        }
        if (_loadedData == null)
        {
            Debug.Log("기반 저장 데이터가 없어 전체 저장으로 전환합니다.");
            await SaveAsync(slot);
            return;
        }

        _isSaving = true;

        try
        {
            if (_loadedData.Players == null)
                _loadedData.Players = new List<PlayerSaveData>();

            _loadedData.Players.RemoveAll(p => p != null && p.PlayerId == playerId);
            _loadedData.Players.Add(playerSave);

            await _repository.SaveAsync(_loadedData, slot);
        }
        finally
        {
            _isSaving = false;
            await FlushPendingSaves();
        }
    }

    [PunRPC]
    private void RPC_RequestPlayerOnlySave(string playerId, string json, int slot, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PlayerSaveData saveData = JsonUtility.FromJson<PlayerSaveData>(json);
        if (saveData == null) return;

        SavePlayerOnlyAsync(playerId, saveData, slot).Forget();
    }

    public UniTask<bool> HasSaveAsync(int slot = 0) => _repository.HasSaveAsync(slot);

    public bool HasPlayerData(string playerId)
    {
        if (_loadedData == null) return false;
        return _loadedData.Players.Exists(p => p.PlayerId == playerId);
    }

    private PlayerController GetLocalPlayer()
    {
        foreach (var player in _players.Values)
        {
            if (player != null && player.IsMine) return player;
        }

        return null;
    }
}
