using Cysharp.Threading.Tasks;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject _titlePanel;
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _joinPanel;
    [SerializeField] private GameObject _roomPanel;
    [SerializeField] private GameObject _connectingPanel;

    [Header("로비 - 세이브 슬롯")]
    [SerializeField] private Transform _saveSlotParent;
    [SerializeField] private TitleSaveSlot _saveSlotPrefab;
    [SerializeField] private int _maxSlots = 3;

    [Header("방 참여")]
    [SerializeField] private TMP_InputField _roomIdInput;
    [SerializeField] private TMP_Text _joinErrorText;

    [Header("방 생성 완료")]
    [SerializeField] private TMP_Text _createdRoomIdText;

    [Header("씬 이름")]
    [SerializeField] private string _customizeSceneName = "YJ_Customizing";
    [SerializeField] private string _gameSceneName = "GameScene";

    private int _selectedSlot;

    private void Start()
    {
        ShowPanel(_titlePanel);
    }

    // === Title Panel ===

    public void OnClickGameStart()
    {
        ShowPanel(_lobbyPanel);
        RefreshSaveSlots().Forget();
    }

    // === Lobby Panel ===

    private async UniTaskVoid RefreshSaveSlots()
    {
        // 기존 슬롯 제거
        foreach (Transform child in _saveSlotParent)
            Destroy(child.gameObject);

        var repo = new LocalJsonSaveRepository();

        for (int i = 0; i < _maxSlots; i++)
        {
            bool exists = await repo.HasSaveAsync(i);
            var slot = Instantiate(_saveSlotPrefab, _saveSlotParent);
            int slotIndex = i;
            slot.Setup(slotIndex, exists, () => _selectedSlot = slotIndex);
        }
    }

    public void OnClickCreateRoom()
    {
        ShowPanel(_connectingPanel);

        NetworkManager.Instance.Connect(
            onConnected: () =>
            {
                NetworkManager.Instance.CreateRoom(
                    onJoined: () =>
                    {
                        ShowPanel(_roomPanel);
                        _createdRoomIdText.text = NetworkManager.Instance.RoomId;
                    },
                    onFailed: () =>
                    {
                        ShowPanel(_lobbyPanel);
                    }
                );
            },
            onFailed: () =>
            {
                ShowPanel(_lobbyPanel);
            }
        );
    }

    public void OnClickJoinRoom()
    {
        ShowPanel(_joinPanel);
        _roomIdInput.text = "";
        if (_joinErrorText != null) _joinErrorText.text = "";
    }

    // === Room Panel (방 생성 완료) ===

    public void OnClickRoomStart()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.LoadLevel(_customizeSceneName);
    }

    // === Join Panel ===

    public void OnClickConfirmJoin()
    {
        string roomId = _roomIdInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(roomId))
        {
            if (_joinErrorText != null) _joinErrorText.text = "방 ID를 입력해주세요.";
            return;
        }

        ShowPanel(_connectingPanel);

        NetworkManager.Instance.Connect(
            onConnected: () =>
            {
                NetworkManager.Instance.JoinRoom(roomId,
                    onJoined: () =>
                    {
                        PhotonNetwork.AutomaticallySyncScene = true;
                        NetworkManager.Instance.CheckFirstVisit(
                            onFirstVisit: () => PhotonNetwork.LoadLevel(_customizeSceneName),
                            onReturning: () => PhotonNetwork.LoadLevel(_gameSceneName)
                        );
                    },
                    onFailed: () =>
                    {
                        ShowPanel(_joinPanel);
                        if (_joinErrorText != null)
                            _joinErrorText.text = "방을 찾을 수 없습니다.";
                    }
                );
            },
            onFailed: () =>
            {
                ShowPanel(_joinPanel);
                if (_joinErrorText != null)
                    _joinErrorText.text = "서버 연결에 실패했습니다.";
            }
        );
    }

    public void OnClickJoinBack()
    {
        ShowPanel(_lobbyPanel);
    }

    // === Lobby Panel 뒤로가기 ===

    public void OnClickLobbyBack()
    {
        ShowPanel(_titlePanel);
    }

    // === 유틸 ===

    private void ShowPanel(GameObject panel)
    {
        _titlePanel.SetActive(panel == _titlePanel);
        _lobbyPanel.SetActive(panel == _lobbyPanel);
        _joinPanel.SetActive(panel == _joinPanel);
        _roomPanel.SetActive(panel == _roomPanel);
        _connectingPanel.SetActive(panel == _connectingPanel);
    }
}