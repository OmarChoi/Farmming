using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance { get; private set; }

    public string LocalPlayerId => _authProvider.PlayerId;

    private IAuthProvider _authProvider;

    private Action _onConnectedCallback;
    private Action _onFailedCallback;

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

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"연결 끊김: {cause}");
        _onFailedCallback?.Invoke();
        _onConnectedCallback = null;
        _onFailedCallback = null;
    }

    public string GetPlayerId()
    {
        return LocalPlayerId;
    }
}