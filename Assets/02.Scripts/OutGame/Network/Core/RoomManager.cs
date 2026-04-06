using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance { get; private set; }

    private const string VISITED_KEY = "vp";

    public enum ERoomAction { None, Create, Join }

    public string RoomId { get; private set; }
    public bool IsFirstVisit { get; set; }
    public int SelectedSlot { get; set; }

    public ERoomAction PendingAction { get; set; }
    public string PendingRoomId { get; set; }

    private Action _onJoinedCallback;
    private Action _onFailedCallback;
    private Action _onLeftRoomCallback;
    private Action _onFirstVisit;
    private Action _onReturning;
    private bool _loadTitleSceneOnLeftRoom = true;

    private readonly Dictionary<string, RoomInfo> _cachedRoomList = new();
    private Action<bool> _onRoomCheckResult;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void CreateRoom(Action onJoined, Action onFailed = null, bool isFirstVisit = true,
        List<string> visitedPlayerIds = null)
    {
        _onJoinedCallback = onJoined;
        _onFailedCallback = onFailed;
        RoomId = GenerateRoomId();
        IsFirstVisit = isFirstVisit;

        string vp = visitedPlayerIds != null ? string.Join(",", visitedPlayerIds) : "";

        var options = new RoomOptions
        {
            MaxPlayers = 4,
            IsVisible = true,
            IsOpen = false,
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { VISITED_KEY, vp } },
            CustomRoomPropertiesForLobby = new[] { VISITED_KEY }
        };

        PhotonNetwork.CreateRoom(RoomId, options);
    }

    public void JoinRoom(string roomId, Action onJoined, Action onFailed = null)
    {
        _onJoinedCallback = onJoined;
        _onFailedCallback = onFailed;
        RoomId = roomId;

        PhotonNetwork.JoinRoom(roomId);
    }

    public void LeaveRoom(Action onLeftRoom = null, bool loadTitleScene = true)
    {
        _onLeftRoomCallback = onLeftRoom;
        _loadTitleSceneOnLeftRoom = loadTitleScene;

        if (!PhotonNetwork.InRoom)
        {
            _onLeftRoomCallback?.Invoke();
            _onLeftRoomCallback = null;
            return;
        }

        PhotonNetwork.LeaveRoom();
    }

    public override void OnJoinedRoom()
    {
        _onJoinedCallback?.Invoke();
        _onJoinedCallback = null;
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 생성 실패: {message}");
        _onFailedCallback?.Invoke();
        _onFailedCallback = null;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 참가 실패: {message}");
        _onFailedCallback?.Invoke();
        _onFailedCallback = null;
    }

    /// 마스터가 나가면 방 폐쇄 — 클라이언트 전원 타이틀로 복귀
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // 마스터가 바뀌었다 = 원래 마스터가 나갔다 → 방 나가고 타이틀로
        Debug.LogWarning("방장이 퇴장하여 방을 종료합니다.");
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        RoomId = null;
        PendingRoomId = null;
        PendingAction = ERoomAction.None;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _onLeftRoomCallback?.Invoke();
        _onLeftRoomCallback = null;

        if (_loadTitleSceneOnLeftRoom)
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName.Title);
    }

    /// 마스터가 GameScene에 도착한 후 호출 — 클라이언트 입장 허용
    public void OpenRoom()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
            PhotonNetwork.CurrentRoom.IsOpen = true;
    }

    // === 방 목록 조회 ===

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (var room in roomList)
        {
            if (room.RemovedFromList)
                _cachedRoomList.Remove(room.Name);
            else
                _cachedRoomList[room.Name] = room;
        }

        if (_onRoomCheckResult != null)
        {
            bool exists = _cachedRoomList.ContainsKey(PendingRoomId);
            _onRoomCheckResult.Invoke(exists);
            _onRoomCheckResult = null;
        }
    }

    public void CheckRoomExists(string roomId, Action<bool> onResult)
    {
        if (_cachedRoomList.ContainsKey(roomId))
        {
            onResult?.Invoke(true);
            return;
        }

        PendingRoomId = roomId;
        _onRoomCheckResult = onResult;
    }

    // === 첫 방문 체크 ===

    public void CheckFirstVisit(Action onFirstVisit, Action onReturning)
    {
        string playerId = NetworkManager.Instance.LocalPlayerId;

        if (PhotonNetwork.IsMasterClient)
        {
            bool exists = SaveManager.Instance != null
                && SaveManager.Instance.HasPlayerData(playerId);

            IsFirstVisit = !exists;

            if (IsFirstVisit) onFirstVisit?.Invoke();
            else onReturning?.Invoke();
        }
        else
        {
            _onFirstVisit = onFirstVisit;
            _onReturning = onReturning;
            photonView.RPC(nameof(RPC_CheckPlayerData), RpcTarget.MasterClient, playerId);
        }
    }

    [PunRPC]
    private void RPC_CheckPlayerData(string playerId, PhotonMessageInfo info)
    {
        bool exists = SaveManager.Instance != null
            && SaveManager.Instance.HasPlayerData(playerId);

        photonView.RPC(nameof(RPC_PlayerDataResult), info.Sender, !exists);
    }

    [PunRPC]
    private void RPC_PlayerDataResult(bool isFirstVisit)
    {
        IsFirstVisit = isFirstVisit;

        if (isFirstVisit)
            _onFirstVisit?.Invoke();
        else
            _onReturning?.Invoke();

        _onFirstVisit = null;
        _onReturning = null;
    }

    /// 로비에서 방 목록의 커스텀 프로퍼티를 확인하여 재방문 여부 판별
    public bool IsReturningPlayer(string roomId, string playerId)
    {
        if (!_cachedRoomList.TryGetValue(roomId, out var info)) return false;
        if (!info.CustomProperties.TryGetValue(VISITED_KEY, out var val)) return false;
        if (val is not string players) return false;
        return players.Contains(playerId);
    }

    /// 저장 완료 후 호출 — 방 커스텀 프로퍼티에 방문 플레이어 목록 갱신
    public void UpdateVisitedPlayers(List<string> playerIds)
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) return;
        string joined = string.Join(",", playerIds);
        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new ExitGames.Client.Photon.Hashtable { { VISITED_KEY, joined } });
    }

    private string GenerateRoomId()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var sb = new System.Text.StringBuilder(6);
        for (int i = 0; i < 6; i++)
            sb.Append(chars[UnityEngine.Random.Range(0, chars.Length)]);
        return sb.ToString();
    }
}
