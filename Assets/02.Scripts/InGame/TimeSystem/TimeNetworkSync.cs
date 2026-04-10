using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class TimeNetworkSync : MonoBehaviourPunCallbacks
{
    private TimeSystem TimeSystem => TimeSystem.Instance;

    public override void OnEnable()
    {
        if (TimeSystem != null)
        {
            TimeSystem.RegisterSync(this);
        }
    }

    private void Start()
    {
        if (TimeSystem != null)
        {
            TimeSystem.RegisterSync(this);
        }
    }

    public override void OnDisable()
    {
        if (TimeSystem != null)
        {
            TimeSystem.UnregisterSync(this);
        }
    }

    public void SendSkipToNextDayRequest()
    {
        photonView.RPC(nameof(RPC_RequestSkipToNextDay), RpcTarget.MasterClient);
    }

    public void SendSyncTime(int day, int hour, int minute)
    {
        photonView.RPC(nameof(RPC_SyncTime), RpcTarget.Others, day, hour, minute);
    }

    public void SendBroadcastEvent(TimeEvents.EventType eventType)
    {
        photonView.RPC(nameof(RPC_ExecuteEvent), RpcTarget.Others, (byte)eventType);
    }

    [PunRPC]
    private void RPC_RequestSkipToNextDay()
    {
        if (TimeSystem == null || !TimeSystem.HasTimeAuthority) return;
        TimeSystem.ExecuteAuthoritySkipToNextDay();
    }

    [PunRPC]
    private void RPC_SyncTime(int day, int hour, int minute)
    {
        if (TimeSystem == null) return;
        TimeSystem.ApplySyncTime(day, hour, minute);
    }

    [PunRPC]
    private void RPC_ExecuteEvent(byte eventTypeByte)
    {
        if (TimeSystem == null) return;
        TimeSystem.ApplyRemoteEvent((TimeEvents.EventType)eventTypeByte);
    }
}
