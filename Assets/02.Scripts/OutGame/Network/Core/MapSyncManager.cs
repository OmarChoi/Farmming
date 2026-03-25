using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

public class MapSyncManager : MonoBehaviourPunCallbacks
{
    public static MapSyncManager Instance { get; private set; }

    [SerializeField] private TerrainGridManager _terrainGridManager;
    [SerializeField] private MapNavMeshController _mapNavMeshController;

    private const int CHUNK_SIZE = 4096;
    private const int SEND_INTERVAL_MS = 50;

    private readonly Dictionary<int, byte[][]> _pendingChunks = new();

    public event Action OnMapSynced;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// 마스터가 특정 플레이어에게 현재 맵 데이터를 전송
    public void SendMapTo(Photon.Realtime.Player target)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        SendChunksAsync(target).Forget();
    }

    /// 마스터가 모든 클라이언트에게 맵 전송
    public void BroadcastMap()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        BroadcastChunksAsync().Forget();
    }

    private async UniTaskVoid SendChunksAsync(Photon.Realtime.Player target)
    {
        byte[] compressed = CompressMapData();
        int totalChunks = Mathf.CeilToInt((float)compressed.Length / CHUNK_SIZE);
        int syncId = UnityEngine.Random.Range(0, int.MaxValue);

        Debug.Log($"맵 동기화 전송: 압축 {compressed.Length} bytes, {totalChunks} 청크");

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
        var saveData = _terrainGridManager.ExportSaveData();
        string json = JsonUtility.ToJson(saveData);
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
        Debug.Log($"맵 동기화 시작 (청크 {totalChunks}개)");
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
        var saveData = JsonUtility.FromJson<TerrainSaveData>(json);

        _terrainGridManager.ImportSaveData(saveData);

        Debug.Log($"맵 동기화 완료 (압축 {compressed.Length} bytes)");
        OnMapSynced?.Invoke();
    }

    /// 클라이언트가 GameScene에 도착한 후 마스터에게 맵 요청
    public void RequestMapFromMaster()
    {
        if (PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_RequestMap), RpcTarget.MasterClient);
    }

    [PunRPC]
    private void RPC_RequestMap(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        SendMapTo(info.Sender);
    }
}