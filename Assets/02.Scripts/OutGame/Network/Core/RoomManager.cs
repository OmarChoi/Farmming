using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance { get; private set; }

    public enum ERoomAction { None, Create, Join }

    public string RoomId { get; private set; }
    public bool IsFirstVisit { get; set; }
    public int SelectedSlot { get; set; }

    public ERoomAction PendingAction { get; set; }
    public string PendingRoomId { get; set; }

    private Action _onJoinedCallback;
    private Action _onFailedCallback;
    private Action _onFirstVisit;
    private Action _onReturning;

    private readonly Dictionary<string, RoomInfo> _cachedRoomList = new();
    private Action<bool> _onRoomCheckResult;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void CreateRoom(Action onJoined, Action onFailed = null)
    {
        _onJoinedCallback = onJoined;
        _onFailedCallback = onFailed;
        RoomId = GenerateRoomId();
        IsFirstVisit = true;

        var options = new RoomOptions
        {
            MaxPlayers = 4,
            IsVisible = true,
            IsOpen = false
        };

        PhotonNetwork.CreateRoom(RoomId, options);
    }

    public void JoinRoom(string roomId, Action onJoined, Action onFailed = null)
    {
        _onJoinedCallback = onJoined;
        _onFailedCallback = onFailed;
        RoomId = roomId;

        PhotonNetwork.JoinRoom(roomId);
    }

    public override void OnJoinedRoom()
    {
        _onJoinedCallback?.Invoke();
        _onJoinedCallback = null;
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 생성 실패: {message}");
        _onFailedCallback?.Invoke();
        _onFailedCallback = null;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 참가 실패: {message}");
        _onFailedCallback?.Invoke();
        _onFailedCallback = null;
    }

    /// 마스터가 GameScene에 도착한 후 호출 — 클라이언트 입장 허용
    public void OpenRoom()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
            PhotonNetwork.CurrentRoom.IsOpen = true;
    }

    // === 방 목록 조회 ===

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (var room in roomList)
        {
            if (room.RemovedFromList)
                _cachedRoomList.Remove(room.Name);
            else
                _cachedRoomList[room.Name] = room;
        }

        if (_onRoomCheckResult != null)
        {
            bool exists = _cachedRoomList.ContainsKey(PendingRoomId);
            _onRoomCheckResult.Invoke(exists);
            _onRoomCheckResult = null;
        }
    }

    public void CheckRoomExists(string roomId, Action<bool> onResult)
    {
        if (_cachedRoomList.ContainsKey(roomId))
        {
            onResult?.Invoke(true);
            return;
        }

        PendingRoomId = roomId;
        _onRoomCheckResult = onResult;
    }

    // === 첫 방문 체크 ===

    public void CheckFirstVisit(Action onFirstVisit, Action onReturning)
    {
        string playerId = NetworkManager.Instance.LocalPlayerId;

        if (PhotonNetwork.IsMasterClient)
        {
            bool exists = SaveManager.Instance != null
                && SaveManager.Instance.HasPlayerData(playerId);

            IsFirstVisit = !exists;

            if (IsFirstVisit) onFirstVisit?.Invoke();
            else onReturning?.Invoke();
        }
        else
        {
            _onFirstVisit = onFirstVisit;
            _onReturning = onReturning;
            photonView.RPC(nameof(RPC_CheckPlayerData), RpcTarget.MasterClient, playerId);
        }
    }

    [PunRPC]
    private void RPC_CheckPlayerData(string playerId, PhotonMessageInfo info)
    {
        bool exists = SaveManager.Instance != null
            && SaveManager.Instance.HasPlayerData(playerId);

        photonView.RPC(nameof(RPC_PlayerDataResult), info.Sender, !exists);
    }

    [PunRPC]
    private void RPC_PlayerDataResult(bool isFirstVisit)
    {
        IsFirstVisit = isFirstVisit;

        if (isFirstVisit)
            _onFirstVisit?.Invoke();
        else
            _onReturning?.Invoke();

        _onFirstVisit = null;
        _onReturning = null;
    }

    private string GenerateRoomId()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var sb = new System.Text.StringBuilder(6);
        for (int i = 0; i < 6; i++)
            sb.Append(chars[UnityEngine.Random.Range(0, chars.Length)]);
        return sb.ToString();
    }
}