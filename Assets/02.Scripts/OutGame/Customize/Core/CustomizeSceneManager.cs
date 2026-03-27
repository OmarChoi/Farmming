using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomizeSceneManager : MonoBehaviour
{
    [SerializeField] private CharacterPartSwapper _partSwapper;

    public void OnConfirm()
    {
        CustomizeData.Instance.CopyFrom(_partSwapper);

        if (RoomManager.Instance == null || RoomManager.Instance.PendingAction == RoomManager.ERoomAction.None)
        {
            SceneManager.LoadScene(SceneName.Game);
            return;
        }

        var action = RoomManager.Instance.PendingAction;
        RoomManager.Instance.PendingAction = RoomManager.ERoomAction.None;

        if (action == RoomManager.ERoomAction.Create)
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.LoadLevel(SceneName.Game);
        }
        else
        {
            // 첫 방문 클라이언트: 커스터마이징 후 방 입장
            string roomId = RoomManager.Instance.PendingRoomId;
            RoomManager.Instance.JoinRoom(roomId,
                onJoined: () => PhotonNetwork.LoadLevel(SceneName.Game)
            );
        }
    }
}