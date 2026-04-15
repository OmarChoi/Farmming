using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomizeSceneManager : MonoBehaviour
{
    [SerializeField] private CharacterPartSwapper _partSwapper;

    public void OnConfirm()
    {
        Confirm();
    }

    private void Confirm()
    {
        CustomizeData.Instance.CopyFrom(_partSwapper);

        if (RoomManager.Instance == null || RoomManager.Instance.PendingAction == RoomManager.ERoomAction.None)
        {
            SceneManager.LoadScene(SceneName.Game);
            return;
        }

        var action = RoomManager.Instance.PendingAction;

        if (action == RoomManager.ERoomAction.Create)
        {
            // 방 생성자는 RoomManager.CreateRoom에서 preload를 이미 통과했으므로 바로 게임 씬 로드를 시작한다.
            RoomManager.Instance.PendingAction = RoomManager.ERoomAction.None;
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.LoadLevel(SceneName.Game);
        }
        else
        {
            // 첫 참가자는 JoinRoom 전에 RoomManager의 preload 게이트를 타도록 PendingAction을 정리하고 입장한다.
            RoomManager.Instance.PendingAction = RoomManager.ERoomAction.None;
            string roomId = RoomManager.Instance.PendingRoomId;
            RoomManager.Instance.JoinRoom(
                roomId,
                onJoined: () => PhotonNetwork.LoadLevel(SceneName.Game),
                onFailed: HandleRoomJoinFailed);
        }
    }

    /// <summary>
    /// 커스터마이즈 이후 방 참가 실패 시 타이틀로 돌아가 사용자가 재시도할 수 있게 한다.
    /// </summary>
    private void HandleRoomJoinFailed()
    {
        // JoinRoom의 preload 실패 또는 Photon 참가 실패는 커스터마이즈 씬에서 복구 UI가 없으므로 타이틀로 복귀한다.
        Debug.LogError("[CustomizeSceneManager] Room join failed.");
        SceneManager.LoadScene(SceneName.Title);
    }
}
