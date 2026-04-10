using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class MapSyncManager : MonoBehaviourPunCallbacks
{
    public static MapSyncManager Instance { get; private set; }

    [SerializeField] private TerrainGridManager _terrainGridManager;

    private const int CHUNK_SIZE = 4096;
    private const int SEND_INTERVAL_MS = 50;
    private const string PropTerrainReady = "tRdy";

    private readonly Dictionary<int, byte[][]> _pendingChunks = new();
    private readonly List<Photon.Realtime.Player> _pendingMapRequests = new();
    private readonly HashSet<int> _villageCacheReadyActors = new();
    private readonly HashSet<int> _pendingTerrainReplayActors = new();
    private readonly Dictionary<Vector3Int, TerrainCellDelta> _pendingTerrainReplayDeltas = new();
    private bool _mapReady = true;
    private int _villageCacheToken;

    private const float VillageCachePrepareTimeoutSeconds = 5f;

    public event Action OnMapSynced;

    private int _dungeonSeed;
    private int _dungeonFloor;
    private bool _dungeonSeedReady;
    public event Action<int, int> OnDungeonSeedReceived;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SendMapTo(Photon.Realtime.Player target)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        ResetTerrainReadyState(target);
        SendChunksAsync(target).Forget();
    }

    public void BroadcastTerrainCellStateFromMaster(Vector3Int position)
    {
        BroadcastTerrainCellStatesFromMaster(position);
    }

    public void BroadcastTerrainCellStatesFromMaster(params Vector3Int[] positions)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (_terrainGridManager == null || positions == null || positions.Length == 0) return;

        var batch = new TerrainCellDeltaBatch();
        var seen = new HashSet<Vector3Int>();

        foreach (Vector3Int position in positions)
        {
            if (!seen.Add(position)) continue;
            if (_terrainGridManager.TryBuildCellDelta(position, out TerrainCellDelta delta))
                batch.Cells.Add(delta);
        }

        if (batch.Cells.Count == 0) return;

        CachePendingTerrainReplay(batch);

        string json = JsonUtility.ToJson(batch);
        foreach (Player player in PhotonNetwork.PlayerListOthers)
        {
            if (player == null) continue;
            if (_pendingTerrainReplayActors.Contains(player.ActorNumber)) continue;

            photonView.RPC(nameof(RPC_ApplyTerrainCellDeltaBatch), player, json);
        }
    }

    /// 마스터의 맵 로드가 완료될 때까지 클라이언트 요청을 대기시킴
    public void HoldRequests()
    {
        _mapReady = false;
        _pendingMapRequests.Clear();
    }

    public void BroadcastMap()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        _mapReady = true;

        foreach (var player in _pendingMapRequests)
            SendMapTo(player);
        _pendingMapRequests.Clear();

        BroadcastChunksAsync().Forget();
    }

    public async UniTask PrepareVillageCacheForDungeonAsync()
    {
        CaptureVillageCacheLocally();

        if (!PhotonNetwork.IsConnected)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        _villageCacheReadyActors.Clear();
        _villageCacheToken = UnityEngine.Random.Range(1, int.MaxValue);
        _villageCacheReadyActors.Add(PhotonNetwork.LocalPlayer.ActorNumber);

        photonView.RPC(nameof(RPC_PrepareVillageCacheForDungeon), RpcTarget.Others, _villageCacheToken);

        float timeout = Time.realtimeSinceStartup + VillageCachePrepareTimeoutSeconds;
        await UniTask.WaitUntil(() =>
            _villageCacheReadyActors.Count >= PhotonNetwork.PlayerList.Length ||
            Time.realtimeSinceStartup > timeout);
    }

    private async UniTaskVoid SendChunksAsync(Photon.Realtime.Player target)
    {
        BeginTerrainReplayCapture(target);
        byte[] compressed = CompressMapData();
        int totalChunks = Mathf.CeilToInt((float)compressed.Length / CHUNK_SIZE);
        int syncId = UnityEngine.Random.Range(0, int.MaxValue);

        Debug.Log($"Map sync send: {compressed.Length} bytes, {totalChunks} chunks");

        photonView.RPC(nameof(RPC_MapSyncStart), target, syncId, totalChunks);

        for (int i = 0; i < totalChunks; i++)
        {
            int start = i * CHUNK_SIZE;
            int length = Mathf.Min(CHUNK_SIZE, compressed.Length - start);
            byte[] chunk = new byte[length];
            Array.Copy(compressed, start, chunk, 0, length);

            photonView.RPC(nameof(RPC_MapSyncChunk), target, syncId, i, chunk);
            await UniTask.Delay(SEND_INTERVAL_MS);
        }

        photonView.RPC(nameof(RPC_MapSyncEnd), target, syncId);
    }

    private async UniTaskVoid BroadcastChunksAsync()
    {
        BeginTerrainReplayCaptureForCurrentOthers();
        byte[] compressed = CompressMapData();
        int totalChunks = Mathf.CeilToInt((float)compressed.Length / CHUNK_SIZE);
        int syncId = UnityEngine.Random.Range(0, int.MaxValue);

        photonView.RPC(nameof(RPC_MapSyncStart), RpcTarget.Others, syncId, totalChunks);

        for (int i = 0; i < totalChunks; i++)
        {
            int start = i * CHUNK_SIZE;
            int length = Mathf.Min(CHUNK_SIZE, compressed.Length - start);
            byte[] chunk = new byte[length];
            Array.Copy(compressed, start, chunk, 0, length);

            photonView.RPC(nameof(RPC_MapSyncChunk), RpcTarget.Others, syncId, i, chunk);
            await UniTask.Delay(SEND_INTERVAL_MS);
        }

        photonView.RPC(nameof(RPC_MapSyncEnd), RpcTarget.Others, syncId);
    }

    private byte[] CompressMapData()
    {
        var syncData = new MapSyncData
        {
            Terrain = _terrainGridManager.ExportSaveData(),
            Buildings = BuildingManager.Instance != null
                ? BuildingManager.Instance.ExportBuildings()
                : new List<BuildingSaveData>(),
            Storages = StorageManager.Instance.ExportStorages()
        };

        string json = JsonUtility.ToJson(syncData);
        byte[] raw = Encoding.UTF8.GetBytes(json);

        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionMode.Compress))
            gz.Write(raw, 0, raw.Length);

        return ms.ToArray();
    }

    private string DecompressMapData(byte[] compressed)
    {
        using var ms = new MemoryStream(compressed);
        using var gz = new GZipStream(ms, CompressionMode.Decompress);
        using var reader = new StreamReader(gz, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    [PunRPC]
    private void RPC_MapSyncStart(int syncId, int totalChunks)
    {
        _pendingChunks[syncId] = new byte[totalChunks][];
        Debug.Log($"Map sync start ({totalChunks} chunks)");
    }

    [PunRPC]
    private void RPC_MapSyncChunk(int syncId, int index, byte[] chunk)
    {
        if (!_pendingChunks.ContainsKey(syncId)) return;
        _pendingChunks[syncId][index] = chunk;
    }

    [PunRPC]
    private void RPC_MapSyncEnd(int syncId)
    {
        if (!_pendingChunks.TryGetValue(syncId, out var chunks)) return;

        int totalLength = 0;
        foreach (var chunk in chunks)
            totalLength += chunk.Length;

        byte[] compressed = new byte[totalLength];
        int offset = 0;
        foreach (var chunk in chunks)
        {
            Array.Copy(chunk, 0, compressed, offset, chunk.Length);
            offset += chunk.Length;
        }

        _pendingChunks.Remove(syncId);

        string json = DecompressMapData(compressed);
        var syncData = JsonUtility.FromJson<MapSyncData>(json);
        ApplySyncedMapData(syncData, compressed.Length).Forget();
    }

    private async UniTaskVoid ApplySyncedMapData(MapSyncData syncData, int payloadSize)
    {
        _terrainGridManager.ImportSaveData(syncData.Terrain);

        if (syncData.Buildings != null && syncData.Buildings.Count > 0 && BuildingManager.Instance != null)
            await BuildingManager.Instance.ImportBuildings(syncData.Buildings);

        StorageManager.Instance.ImportStorages(syncData.Storages);

        Debug.Log($"Map sync completed ({payloadSize} bytes)");
        OnMapSynced?.Invoke();
    }

    public void BroadcastDungeonSeed(int floor, int seed)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        _dungeonFloor = floor;
        _dungeonSeed = seed;
        _dungeonSeedReady = true;
        photonView.RPC(nameof(RPC_DungeonSeed), RpcTarget.Others, floor, seed);
    }

    public void RequestDungeonSeed()
    {
        if (PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_RequestDungeonSeed), RpcTarget.MasterClient);
    }

    [PunRPC]
    private void RPC_DungeonSeed(int floor, int seed)
    {
        OnDungeonSeedReceived?.Invoke(floor, seed);
    }

    [PunRPC]
    private void RPC_RequestDungeonSeed(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!_dungeonSeedReady) return;
        photonView.RPC(nameof(RPC_DungeonSeed), info.Sender, _dungeonFloor, _dungeonSeed);
    }

    public event Action<int> OnDungeonEntryRequested;

    public void RequestDungeonEntry(int floor)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            OnDungeonEntryRequested?.Invoke(floor);
            return;
        }
        photonView.RPC(nameof(RPC_RequestDungeonEntry), RpcTarget.MasterClient, floor);
    }

    [PunRPC]
    private void RPC_RequestDungeonEntry(int floor)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        OnDungeonEntryRequested?.Invoke(floor);
    }

    public void RequestMapFromMaster()
    {
        if (PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_RequestMap), RpcTarget.MasterClient);
    }

    [PunRPC]
    private void RPC_RequestMap(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (!_mapReady)
        {
            _pendingMapRequests.Add(info.Sender);
            return;
        }

        SendMapTo(info.Sender);
    }
    
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (targetPlayer == null || changedProps == null) return;
        if (!_pendingTerrainReplayActors.Contains(targetPlayer.ActorNumber)) return;

        if (!changedProps.TryGetValue(PropTerrainReady, out object value)) return;
        if (value is not bool isReady || !isReady) return;

        FlushPendingTerrainReplayTo(targetPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayer == null) return;

        if (_pendingTerrainReplayActors.Remove(otherPlayer.ActorNumber) &&
            _pendingTerrainReplayActors.Count == 0)
        {
            _pendingTerrainReplayDeltas.Clear();
        }
    }

    [PunRPC]
    private void RPC_ApplyTerrainCellDeltaBatch(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        if (_terrainGridManager == null) return;

        TerrainCellDeltaBatch batch = JsonUtility.FromJson<TerrainCellDeltaBatch>(json);
        if (batch?.Cells == null || batch.Cells.Count == 0) return;

        _terrainGridManager.ApplyCellDeltas(batch.Cells);
        LogAppliedTerrainCellDeltaBatch(batch);
    }

    private void BeginTerrainReplayCapture(Photon.Realtime.Player target)
    {
        if (!PhotonNetwork.IsMasterClient || target == null) return;

        if (_pendingTerrainReplayActors.Count == 0)
            _pendingTerrainReplayDeltas.Clear();

        _pendingTerrainReplayActors.Add(target.ActorNumber);
    }

    private void BeginTerrainReplayCaptureForCurrentOthers()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player[] others = PhotonNetwork.PlayerListOthers;
        if (others == null || others.Length == 0) return;

        if (_pendingTerrainReplayActors.Count == 0)
            _pendingTerrainReplayDeltas.Clear();

        foreach (Player player in others)
        {
            if (player == null) continue;
            _pendingTerrainReplayActors.Add(player.ActorNumber);
        }
    }

    private void CachePendingTerrainReplay(TerrainCellDeltaBatch batch)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (_pendingTerrainReplayActors.Count == 0) return;
        if (batch?.Cells == null || batch.Cells.Count == 0) return;

        foreach (TerrainCellDelta delta in batch.Cells)
        {
            if (delta == null) continue;
            _pendingTerrainReplayDeltas[new Vector3Int(delta.X, delta.Y, delta.Z)] = delta;
        }
    }

    private void FlushPendingTerrainReplayTo(Player target)
    {
        if (!PhotonNetwork.IsMasterClient || target == null) return;
        if (!_pendingTerrainReplayActors.Remove(target.ActorNumber)) return;

        if (_pendingTerrainReplayDeltas.Count > 0)
        {
            var batch = new TerrainCellDeltaBatch
            {
                Cells = new List<TerrainCellDelta>(_pendingTerrainReplayDeltas.Values)
            };

            string json = JsonUtility.ToJson(batch);
            photonView.RPC(nameof(RPC_ApplyTerrainCellDeltaBatch), target, json);
        }

        if (_pendingTerrainReplayActors.Count == 0)
            _pendingTerrainReplayDeltas.Clear();
    }

    private static void ResetTerrainReadyState(Player target)
    {
        if (target == null) return;

        var props = new Hashtable
        {
            { PropTerrainReady, false }
        };
        target.SetCustomProperties(props);
    }

    private static void LogAppliedTerrainCellDeltaBatch(TerrainCellDeltaBatch batch)
    {
#if !UNITY_EDITOR
        return;
#endif
        if (batch?.Cells == null || batch.Cells.Count == 0) return;
        if (PhotonNetwork.IsMasterClient) return;

        int removedCount = 0;
        foreach (TerrainCellDelta delta in batch.Cells)
        {
            if (delta != null && delta.RemoveCell)
                removedCount++;
        }

        bool terrainReady = false;
        if (PhotonNetwork.LocalPlayer != null &&
            PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PropTerrainReady, out object value) &&
            value is bool isReady)
        {
            terrainReady = isReady;
        }

        int updatedCount = batch.Cells.Count - removedCount;
        Debug.Log(
            $"[MapSyncManager] Applied terrain delta batch on client. total={batch.Cells.Count}, updated={updatedCount}, removed={removedCount}, terrainReady={terrainReady}");
    }

    [PunRPC]
    private void RPC_PrepareVillageCacheForDungeon(int token, PhotonMessageInfo info)
    {
        CaptureVillageCacheLocally();

        if (!PhotonNetwork.IsConnected)
            return;

        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        photonView.RPC(nameof(RPC_ReportVillageCacheReady), RpcTarget.MasterClient, token, actorNumber);
    }

    [PunRPC]
    private void RPC_ReportVillageCacheReady(int token, int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (token != _villageCacheToken) return;
        _villageCacheReadyActors.Add(actorNumber);
    }

    private static void CaptureVillageCacheLocally()
    {
        TerrainGridManager gridManager = TerrainGridManager.Instance;
        if (gridManager == null && MapManager.Instance != null)
            gridManager = MapManager.Instance.GridManager;

        if (gridManager == null) return;

        VillageCache.Capture(gridManager);
        VillageCache.CapturePlayerPositions();
    }
}
