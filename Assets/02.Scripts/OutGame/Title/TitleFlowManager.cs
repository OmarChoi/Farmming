using System;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ETitlePanel { Title, Lobby, Join, Room, Connecting }

public class TitleFlowManager : MonoBehaviour
{
    [SerializeField] private int _maxSlots = 3;

    public event Action<ETitlePanel> OnPanelChanged;
    public event Action<string> OnRoomIdReady;
    public event Action<string> OnJoinError;

    public SaveSlotService SlotService { get; private set; }

    private void Awake()
    {
        SlotService = new SaveSlotService(new LocalJsonSaveRepository(), _maxSlots);
    }

    public async UniTask RefreshSlotsAsync()
    {
        await SlotService.RefreshAsync();
    }

    /// 세이브 슬롯 삭제 → 슬롯 목록 갱신 요청
    public async UniTask DeleteSlotAsync(int slot)
    {
        await SlotService.DeleteAsync(slot);
    }

    /// 새 게임: 첫 번째 빈 슬롯 자동 선택 → 접속 → 방 생성
    public void CreateNewGame()
    {
        int emptySlot = SlotService.FindFirstEmptySlot();
        if (emptySlot < 0) return;

        OnPanelChanged?.Invoke(ETitlePanel.Connecting);
        RoomManager.Instance.SelectedSlot = emptySlot;

        NetworkManager.Instance.Connect(
            onConnected: () =>
            {
                RoomManager.Instance.CreateRoom(
                    onJoined: () =>
                    {
                        OnRoomIdReady?.Invoke(RoomManager.Instance.RoomId);
                        OnPanelChanged?.Invoke(ETitlePanel.Room);
                    },
                    onFailed: () => OnPanelChanged?.Invoke(ETitlePanel.Lobby)
                );
            },
            onFailed: () => OnPanelChanged?.Invoke(ETitlePanel.Lobby)
        );
    }

    /// 기존 세이브 로드: 해당 슬롯 → 접속 → 방 생성 (isFirstVisit = false)
    public async UniTaskVoid LoadExistingGame(int slot)
    {
        OnPanelChanged?.Invoke(ETitlePanel.Connecting);
        RoomManager.Instance.SelectedSlot = slot;

        var visitedPlayerIds = await SlotService.GetPlayerIdsAsync(slot);

        NetworkManager.Instance.Connect(
            onConnected: () =>
            {
                RoomManager.Instance.CreateRoom(
                    onJoined: () =>
                    {
                        OnRoomIdReady?.Invoke(RoomManager.Instance.RoomId);
                        OnPanelChanged?.Invoke(ETitlePanel.Room);
                    },
                    onFailed: () => OnPanelChanged?.Invoke(ETitlePanel.Lobby),
                    isFirstVisit: false,
                    visitedPlayerIds: visitedPlayerIds
                );
            },
            onFailed: () => OnPanelChanged?.Invoke(ETitlePanel.Lobby)
        );
    }

    /// 룸 패널 → 시작: 새 게임이면 커스터마이즈, 로드면 바로 게임씬
    public void StartRoom()
    {
        RoomManager.Instance.PendingAction = RoomManager.ERoomAction.Create;

        if (RoomManager.Instance.IsFirstVisit)
        {
            SceneManager.LoadScene(SceneName.Customize);
        }
        else
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.LoadLevel(SceneName.Game);
        }
    }

    /// 방 참가 확인: roomId 검증 → 접속 → 로비에서 재방문 체크 → 분기
    public void OnClickRoomBack()
    {
        OnPanelChanged?.Invoke(ETitlePanel.Connecting);

        RoomManager.Instance.LeaveRoom(
            onLeftRoom: () => OnPanelChanged?.Invoke(ETitlePanel.Lobby),
            loadTitleScene: false
        );
    }

    public void ConfirmJoin(string roomId)
    {
        if (string.IsNullOrEmpty(roomId))
        {
            OnJoinError?.Invoke("방 ID를 입력해주세요.");
            return;
        }

        OnPanelChanged?.Invoke(ETitlePanel.Connecting);

        NetworkManager.Instance.Connect(
            onConnected: () =>
            {
                RoomManager.Instance.CheckRoomExists(roomId, exists =>
                {
                    if (!exists)
                    {
                        OnPanelChanged?.Invoke(ETitlePanel.Join);
                        OnJoinError?.Invoke("방을 찾을 수 없습니다.");
                        return;
                    }

                    string playerId = NetworkManager.Instance.LocalPlayerId;
                    bool returning = RoomManager.Instance.IsReturningPlayer(roomId, playerId);
                    RoomManager.Instance.PendingRoomId = roomId;
                    RoomManager.Instance.PendingAction = RoomManager.ERoomAction.Join;
                    RoomManager.Instance.IsFirstVisit = !returning;

                    if (returning)
                    {
                        // 재방문 → 방 입장 → 게임씬
                        RoomManager.Instance.JoinRoom(roomId,
                            onJoined: () => PhotonNetwork.LoadLevel(SceneName.Game),
                            onFailed: () =>
                            {
                                OnPanelChanged?.Invoke(ETitlePanel.Join);
                                OnJoinError?.Invoke("방 참가에 실패했습니다.");
                            }
                        );
                    }
                    else
                    {
                        // 첫 방문 → 커스터마이징 → 커스터마이징 끝나면 방 입장
                        SceneManager.LoadScene(SceneName.Customize);
                    }
                });
            },
            onFailed: () =>
            {
                OnPanelChanged?.Invoke(ETitlePanel.Join);
                OnJoinError?.Invoke("서버 연결에 실패했습니다.");
            }
        );
    }
}
