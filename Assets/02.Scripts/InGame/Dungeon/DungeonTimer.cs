using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonTimer : MonoBehaviourPunCallbacks
{
    public static DungeonTimer Instance { get; private set; }

    public float RemainingSeconds { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action<float> OnTimeUpdated;
    public event Action OnTimeExpired;

    private float _endTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StartTimer(float timeLimitSeconds)
    {
        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC(nameof(RPC_StartTimer), RpcTarget.All, timeLimitSeconds);
        }
        else
        {
            BeginCountdown(timeLimitSeconds);
        }
    }

    [PunRPC]
    private void RPC_StartTimer(float timeLimitSeconds)
    {
        BeginCountdown(timeLimitSeconds);
    }

    private void BeginCountdown(float timeLimitSeconds)
    {
        _endTime = Time.realtimeSinceStartup + timeLimitSeconds;
        RemainingSeconds = timeLimitSeconds;
        IsRunning = true;
    }

    private void Update()
    {
        if (!IsRunning) return;

        RemainingSeconds = Mathf.Max(0f, _endTime - Time.realtimeSinceStartup);
        OnTimeUpdated?.Invoke(RemainingSeconds);

        if (RemainingSeconds <= 0f)
        {
            IsRunning = false;
            OnTimeExpired?.Invoke();
            ReturnToVillage();
        }
    }

    private void ReturnToVillage()
    {
        SceneTransitionData.Type = ETransitionType.DungeonToVillage;
        GameSceneInit.ReturningFromDungeon = true;

        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
            {
                var roomProps = new Hashtable
                {
                    { SceneTransitionRoomProps.TransitionType, (int)ETransitionType.DungeonToVillage }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }

            PhotonNetwork.LoadLevel(SceneName.Loading);
        }
        else
        {
            SceneManager.LoadScene(SceneName.Loading);
        }
    }
}