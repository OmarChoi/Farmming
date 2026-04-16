using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class WorldEffectNetworkSync : MonoBehaviourPunCallbacks
{
    private WorldEffectManager Manager => WorldEffectManager.Instance;

    public override void OnEnable()
    {
        base.OnEnable();
        if (Manager != null) Manager.RegisterSync(this);
    }

    private void Start()
    {
        if (Manager != null) Manager.RegisterSync(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (Manager != null) Manager.UnregisterSync(this);
    }

    public void RequestAddEffect(string effectId, byte kindByte, int durationDays)
    {
        photonView.RpcSafe(nameof(RPC_RequestAddEffect), RpcTarget.MasterClient, effectId, kindByte, durationDays);
    }

    public void RequestRemoveEffect(string effectId)
    {
        photonView.RpcSafe(nameof(RPC_RequestRemoveEffect), RpcTarget.MasterClient, effectId);
    }

    public void BroadcastSnapshot(WorldEffectSaveData saveData)
    {
        if (saveData == null) return;

        string json = JsonUtility.ToJson(saveData);
        photonView.RpcSafe(nameof(RPC_ApplySnapshot), RpcTarget.Others, json);
    }

    [PunRPC]
    private void RPC_RequestAddEffect(string effectId, byte kindByte, int durationDays)
    {
        if (Manager == null || !PhotonNetwork.IsMasterClient) return;
        Manager.AddEffect(effectId, (EWorldEffectKind)kindByte, durationDays);
    }

    [PunRPC]
    private void RPC_RequestRemoveEffect(string effectId)
    {
        if (Manager == null || !PhotonNetwork.IsMasterClient) return;
        Manager.RemoveEffect(effectId);
    }

    [PunRPC]
    private void RPC_ApplySnapshot(string json)
    {
        if (Manager == null || PhotonNetwork.IsMasterClient) return;
        if (string.IsNullOrWhiteSpace(json)) return;

        WorldEffectSaveData saveData = JsonUtility.FromJson<WorldEffectSaveData>(json);
        Manager.ApplySnapshot(saveData);
    }
}
