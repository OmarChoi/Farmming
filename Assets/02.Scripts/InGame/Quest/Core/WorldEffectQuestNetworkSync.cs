using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class WorldEffectQuestNetworkSync : MonoBehaviourPunCallbacks
{
    private WorldEffectQuestService Service => WorldEffectQuestService.Instance;

    public override void OnEnable()
    {
        base.OnEnable();
        if (Service != null) Service.RegisterSync(this);
    }

    private void Start()
    {
        if (Service != null) Service.RegisterSync(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (Service != null) Service.UnregisterSync(this);
    }

    public void RequestCompleteAtShrine(string questId)
    {
        photonView.RPC(nameof(RPC_RequestCompleteAtShrine), RpcTarget.MasterClient, questId);
    }

    public void BroadcastSnapshot(ForcedTimedQuestSaveData data)
    {
        if (data == null) return;

        string json = JsonUtility.ToJson(data);
        photonView.RPC(nameof(RPC_ApplySnapshot), RpcTarget.Others, json);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (Service == null || !PhotonNetwork.IsMasterClient) return;

        string json = JsonUtility.ToJson(Service.ExportSaveData());
        photonView.RPC(nameof(RPC_ApplySnapshot), newPlayer, json);
    }

    [PunRPC]
    private void RPC_RequestCompleteAtShrine(string questId)
    {
        if (Service == null || !PhotonNetwork.IsMasterClient) return;
        Service.ExecuteCompleteAtShrine(questId);
    }

    [PunRPC]
    private void RPC_ApplySnapshot(string json)
    {
        if (Service == null || PhotonNetwork.IsMasterClient) return;
        if (string.IsNullOrWhiteSpace(json)) return;

        var data = JsonUtility.FromJson<ForcedTimedQuestSaveData>(json);
        Service.ApplySnapshot(data);
    }
}