using Photon.Pun;

public static class PhotonViewExtensions
{
    public static void RpcSafe(this PhotonView view, string methodName, RpcTarget target, params object[] parameters)
    {
        if (view != null && PhotonNetwork.IsConnected)
            view.RPC(methodName, target, parameters);
    }
}