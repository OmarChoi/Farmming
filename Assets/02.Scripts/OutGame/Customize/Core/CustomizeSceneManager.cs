using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomizeSceneManager : MonoBehaviour
{
    [SerializeField] private CharacterPartSwapper _partSwapper;
    [SerializeField] private string _gameSceneName = "GameScene";

    public void OnConfirm()
    {
        CustomizeData.Instance.CopyFrom(_partSwapper);

        if (RoomManager.Instance == null || RoomManager.Instance.PendingAction == RoomManager.ERoomAction.None)
        {
            SceneManager.LoadScene(_gameSceneName);
            return;
        }

        var action = RoomManager.Instance.PendingAction;
        RoomManager.Instance.PendingAction = RoomManager.ERoomAction.None;

        if (action == RoomManager.ERoomAction.Create)
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.LoadLevel(_gameSceneName);
        }
        else
        {
            string roomId = RoomManager.Instance.PendingRoomId;
            RoomManager.Instance.JoinRoom(roomId,
                onJoined: () =>
                {
                    PhotonNetwork.LoadLevel(_gameSceneName);
                }
            );
        }
    }
}