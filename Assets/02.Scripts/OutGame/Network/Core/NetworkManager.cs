using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance { get; private set; }

    public string RoomId { get; private set; }
    public bool IsFirstVisit { get; set; }
    public string LocalPlayerId => _authProvider.PlayerId;
    public int SelectedSlot { get; set; }

    private IAuthProvider _authProvider;

    private Action _onConnectedCallback;
    private Action _onJoinedCallback;
    private Action _onFailedCallback;
    private Action _onFirstVisit;
    private Action _onReturning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _authProvider = new LocalAuthProvider();
        _authProvider.Init();
    }

    public void Connect(Action onConnected, Action onFailed = null)
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            onConnected?.Invoke();
            return;
        }

        _onConnectedCallback = onConnected;
        _onFailedCallback = onFailed;
        PhotonNetwork.NickName = LocalPlayerId;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        _onConnectedCallback?.Invoke();
        _onConnectedCallback = null;
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
            IsVisible = false,
            IsOpen = true
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

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"연결 끊김: {cause}");
        _onFailedCallback?.Invoke();
        _onConnectedCallback = null;
        _onJoinedCallback = null;
        _onFailedCallback = null;
    }

    public string GetPlayerId()
    {
        return LocalPlayerId;
    }

    public void CheckFirstVisit(Action onFirstVisit, Action onReturning)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 마스터 본인은 로컬에서 확인
            bool exists = SaveManager.Instance != null
                && SaveManager.Instance.HasPlayerData(LocalPlayerId);

            IsFirstVisit = !exists;

            if (IsFirstVisit) onFirstVisit?.Invoke();
            else onReturning?.Invoke();
        }
        else
        {
            // 클라이언트 → 마스터에게 RPC로 확인 요청
            _onFirstVisit = onFirstVisit;
            _onReturning = onReturning;
            photonView.RPC(nameof(RPC_CheckPlayerData), RpcTarget.MasterClient, LocalPlayerId);
        }
    }

    [PunRPC]
    private void RPC_CheckPlayerData(string playerId, PhotonMessageInfo info)
    {
        // 마스터에서 실행
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