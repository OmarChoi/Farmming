using System;
using Cysharp.Threading.Tasks;
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
        ConnectAsync(onConnected, onFailed).Forget();
    }

    /// <summary>
    /// Ensures Addressables-backed PUN prefabs are ready before the Photon connection flow starts.
    /// </summary>
    private async UniTaskVoid ConnectAsync(Action onConnected, Action onFailed)
    {
        // Preload before connecting so JoinRoom cannot replay instantiate events against an empty pool.
        if (!await PunPrefabPoolBootstrap.TryEnsurePreloadedAsync(e =>
            Debug.LogError($"[NetworkManager] Network prefab preload failed before Photon connect.\n{e}")))
        {
            onFailed?.Invoke();
            return;
        }

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
