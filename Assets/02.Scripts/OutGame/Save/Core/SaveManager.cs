using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private TerrainGridManager _terrainGridManager;
    [SerializeField] private MapManager _mapManager;

    private readonly Dictionary<string, PlayerController> _players = new();
    private ISaveRepository _repository;
    private SaveData _loadedData;
    private const float RemoteSaveTimeout = 5f;
    private bool _isSaving;

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

        bool isNewPlayer = _loadedData == null
            || !_loadedData.Players.Exists(p => p.PlayerId == playerId);

        TryRestorePlayer(playerId, player);

        if (isNewPlayer && PhotonNetwork.IsMasterClient)
            SaveAsync(RoomManager.Instance.SelectedSlot).Forget();
    }

    private void TryRestorePlayer(string playerId, PlayerController player)
    {
        if (_loadedData == null) return;
        var save = _loadedData.Players.Find(p => p.PlayerId == playerId);
        if (save == null) return;

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
        if (_isSaving)
        {
            Debug.LogWarning("이미 저장 중입니다.");
            return;
        }

        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("저장은 마스터 클라이언트만 실행할 수 있습니다.");
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

            if (VillageLevelManager.Instance != null)
                data.Village = VillageLevelManager.Instance.ExportBuildings();
            _receivedSaveData.Clear();
            _expectedResponses = 0;

            foreach (var kvp in _players)
            {
                var player = kvp.Value;
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
        }
    }

    public async UniTask LoadAsync(int slot = 0)
    {
        _loadedData = await _repository.LoadAsync(slot);
        if (_loadedData == null)
        {
            Debug.Log($"저장 데이터 없음 (슬롯 {slot})");
            return;
        }

        _mapManager.ImportVillageSaveData(_loadedData.Terrain);

        Debug.Log($"로드 완료 (슬롯 {slot}, 플레이어 데이터 {_loadedData.Players.Count}명)");
    }

    public async UniTask LoadBuildingAsync()
    {
        if (BuildingManager.Instance != null && _loadedData.Buildings != null)
            await BuildingManager.Instance.ImportBuildings(_loadedData.Buildings);
    }

    public UniTask<bool> HasSaveAsync(int slot = 0) => _repository.HasSaveAsync(slot);

    public bool HasPlayerData(string playerId)
    {
        if (_loadedData == null) return false;
        return _loadedData.Players.Exists(p => p.PlayerId == playerId);
    }
}