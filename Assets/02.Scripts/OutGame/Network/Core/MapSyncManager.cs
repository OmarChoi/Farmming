using System;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using UnityEngine;

public class MapSyncManager : MonoBehaviourPunCallbacks
{
    public static MapSyncManager Instance { get; private set; }

    [SerializeField] private TerrainGridManager _terrainGridManager;
    [SerializeField] private MapNavMeshController _mapNavMeshController;

    private const int CHUNK_SIZE = 4096;

    private readonly Dictionary<int, string[]> _pendingChunks = new();

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

        var saveData = _terrainGridManager.ExportSaveData();
        string json = JsonUtility.ToJson(saveData);

        int totalChunks = Mathf.CeilToInt((float)json.Length / CHUNK_SIZE);
        int syncId = UnityEngine.Random.Range(0, int.MaxValue);

        photonView.RPC(nameof(RPC_MapSyncStart), target, syncId, totalChunks);

        for (int i = 0; i < totalChunks; i++)
        {
            int start = i * CHUNK_SIZE;
            int length = Mathf.Min(CHUNK_SIZE, json.Length - start);
            string chunk = json.Substring(start, length);

            photonView.RPC(nameof(RPC_MapSyncChunk), target, syncId, i, chunk);
        }

        photonView.RPC(nameof(RPC_MapSyncEnd), target, syncId);
    }

    /// 마스터가 모든 클라이언트에게 맵 전송
    public void BroadcastMap()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var saveData = _terrainGridManager.ExportSaveData();
        string json = JsonUtility.ToJson(saveData);

        int totalChunks = Mathf.CeilToInt((float)json.Length / CHUNK_SIZE);
        int syncId = UnityEngine.Random.Range(0, int.MaxValue);

        photonView.RPC(nameof(RPC_MapSyncStart), RpcTarget.Others, syncId, totalChunks);

        for (int i = 0; i < totalChunks; i++)
        {
            int start = i * CHUNK_SIZE;
            int length = Mathf.Min(CHUNK_SIZE, json.Length - start);
            string chunk = json.Substring(start, length);

            photonView.RPC(nameof(RPC_MapSyncChunk), RpcTarget.Others, syncId, i, chunk);
        }

        photonView.RPC(nameof(RPC_MapSyncEnd), RpcTarget.Others, syncId);
    }

    [PunRPC]
    private void RPC_MapSyncStart(int syncId, int totalChunks)
    {
        _pendingChunks[syncId] = new string[totalChunks];
        Debug.Log($"맵 동기화 시작 (청크 {totalChunks}개)");
    }

    [PunRPC]
    private void RPC_MapSyncChunk(int syncId, int index, string chunk)
    {
        if (!_pendingChunks.ContainsKey(syncId)) return;
        _pendingChunks[syncId][index] = chunk;
    }

    [PunRPC]
    private void RPC_MapSyncEnd(int syncId)
    {
        if (!_pendingChunks.TryGetValue(syncId, out var chunks)) return;

        var sb = new StringBuilder();
        foreach (var chunk in chunks)
            sb.Append(chunk);

        _pendingChunks.Remove(syncId);

        string json = sb.ToString();
        var saveData = JsonUtility.FromJson<TerrainSaveData>(json);

        _terrainGridManager.ImportSaveData(saveData);

        Debug.Log("맵 동기화 완료 (클라이언트)");
        OnMapSynced?.Invoke();
    }

    // 늦은 접속자: 방에 들어왔을 때 마스터가 자동 전송
    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        SendMapTo(newPlayer);
    }
}