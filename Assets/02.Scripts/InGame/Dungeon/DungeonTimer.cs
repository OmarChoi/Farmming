using ExitGames.Client.Photon;
using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using static TutorialManager;

public class DungeonTimer : MonoBehaviourPunCallbacks
{
    public static DungeonTimer Instance { get; private set; }

    public float RemainingSeconds { get; private set; }
    public bool IsRunning { get; private set; }

    private float _endTime;
    private int _lastDisplayedSeconds = -1;
    
    public event Action<int> OnSecondChanged;
    public event Action OnTimeExpired;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    
    private void Update()
    {
        if (!IsRunning) return;

        RemainingSeconds = Mathf.Max(0f, _endTime - Time.realtimeSinceStartup);

        int displaySeconds = Mathf.CeilToInt(RemainingSeconds);
        if (displaySeconds != _lastDisplayedSeconds)
        {
            _lastDisplayedSeconds = displaySeconds;
            OnSecondChanged?.Invoke(displaySeconds);
        }

        if (RemainingSeconds <= 0f)
        {
            IsRunning = false;
            OnTimeExpired?.Invoke();
            ReturnToVillage();
        }
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


    private void ReturnToVillage()
    {
        DespawnTroublemakers();
        SceneTransitionData.Type = ETransitionType.DungeonToVillage;
        GameSceneInit.ReturningFromDungeon = true;

        if (PhotonNetwork.IsConnected)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            if (PhotonNetwork.CurrentRoom != null)
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

    private void DespawnTroublemakers()
    {
        TroublemakerSpawner spawner = FindFirstObjectByType<TroublemakerSpawner>();
        if (spawner != null)
        {
            spawner.DespawnAll();
        }   
    }
}