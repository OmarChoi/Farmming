using Cysharp.Threading.Tasks;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_Pause : UIBase
{
    private UI_PopupDoTween _popupDoTween;
    private PlayerController _player;
    private bool _prevLockMode;
    private CursorLockMode _prevLockState;
    
    [Header("Buttons")]
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _settingButton;
    [SerializeField] private Button _toLobbyButton;
    [SerializeField] private Button _exitGameButton;
    
    [Header("Room ID")]
    [SerializeField] private TextMeshProUGUI _roomId;

    private void Awake()
    {
        _popupDoTween = GetComponent<UI_PopupDoTween>();
    }
    
    private void OnEnable()
    {
        _continueButton.onClick.AddListener(ContinueGame);
        _settingButton.onClick.AddListener(Setting);
        _toLobbyButton.onClick.AddListener(ChangeSceneToLobby);
        _exitGameButton.onClick.AddListener(ExitGame);
    }

    private void OnDisable()
    {
        _continueButton.onClick.RemoveListener(ContinueGame);
        _settingButton.onClick.RemoveListener(Setting);
        _toLobbyButton.onClick.RemoveListener(ChangeSceneToLobby);
        _exitGameButton.onClick.RemoveListener(ExitGame);
    }
    
    public void SetOwnerPlayer(PlayerController player)
    {
        _player = player;
    }

    protected override void OnOpen()
    {
        if (SceneManager.GetActiveScene().name == SceneName.Loading)
        {
            UIController.Instance.CloseAsync<UI_Pause>().Forget();
            return;
        }

        _prevLockMode = _player.IsActionLocked;
        _player?.LockAction();

        _prevLockState = Cursor.lockState;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        
        if (SceneManager.GetActiveScene().name == SceneName.Title)
        {
            // 메인 화면으로, 방 코드 표시 X
            _toLobbyButton.gameObject.SetActive(false);
            _settingButton.gameObject.SetActive(false);
            RefreshRoomId();
        }
        else
        {
            _toLobbyButton.gameObject.SetActive(true);
            RefreshRoomId();
        }
    }

    protected override void OnClose()
    {
        if (!_prevLockMode)
        {
            _player.UnlockAction();
        }
        Cursor.lockState = _prevLockState;
        Cursor.visible = _prevLockState != CursorLockMode.Locked;
    }

    protected override async UniTask OnOpenAnimation()
    {
        await _popupDoTween.PlayOpenAsync();
    }
    
    protected override async UniTask OnCloseAnimation()
    {
        await _popupDoTween.PlayCloseAsync();
    }
    
    private void RefreshRoomId()
    {
        if (RoomManager.Instance == null)
        {
            _roomId.text = string.Empty;
            return;
        }
        string roomId = RoomManager.Instance.RoomId;
        _roomId.text = $"방 코드 : {roomId}";
    }

    private void ContinueGame()
    {
        UIController.Instance.CloseAsync<UI_Pause>().Forget();
    }

    private void Setting()
    {
        UIController.Instance.OpenAsync<UI_Setting>().Forget();
    }

    private void ChangeSceneToLobby()
    {
        if (PhotonNetwork.IsConnected)
        {
            RoomManager.Instance.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene(SceneName.Title);
        }
    }

    private void ExitGame()
    {
        Application.Quit();
    }
}
