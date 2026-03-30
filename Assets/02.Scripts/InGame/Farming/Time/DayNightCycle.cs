using System;
using Photon.Pun;
using UnityEngine;

public class DayNightCycle : MonoBehaviourPun
{
    public static DayNightCycle Instance;

    public int CurrentDay { get; private set; } = 1;
    public bool IsNight { get; private set; } = false;

    public event Action OnMorningStart;
    public event Action OnNightStart;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
        {
            if(Input.GetKeyDown(KeyCode.N))
            {
                if(!IsNight)
                {
                    ApplyNight();

                    if (PhotonNetwork.IsConnected)
                        photonView.RPC(nameof(RPC_StartNight), RpcTarget.Others);
                }
                else
                {
                    ApplyMorning();

                    if (PhotonNetwork.IsConnected)
                        photonView.RPC(nameof(RPC_StartMorning), RpcTarget.Others, CurrentDay);
                }
            }
        }
    }

    private void ApplyNight()
    {
        IsNight = true;
        Debug.Log($"{CurrentDay}일차 밤 시작");
        OnNightStart?.Invoke();
    }

    private void ApplyMorning()
    {
        IsNight = false;
        CurrentDay++;
        Debug.Log($"{CurrentDay}일차 아침 시작");
        OnMorningStart?.Invoke();
    }

    [PunRPC]
    private void RPC_StartNight()
    {
        ApplyNight();
    }

    [PunRPC]
    private void RPC_StartMorning(int day)
    {
        CurrentDay = day;
        IsNight = false;
        Debug.Log($"{CurrentDay}일차 아침 시작 (동기화)");
        OnMorningStart?.Invoke();
    }
}