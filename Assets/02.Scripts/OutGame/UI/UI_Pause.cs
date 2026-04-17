using Cysharp.Threading.Tasks;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_Pause : UIBase
{
    private UI_PopupDoTween _popupDoTween;

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
        
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        pc.EnterUIMode();
    }

    private void OnDisable()
    {
        _continueButton.onClick.RemoveListener(ContinueGame);
        _settingButton.onClick.RemoveListener(Setting);
        _toLobbyButton.onClick.RemoveListener(ChangeSceneToLobby);
        _exitGameButton.onClick.RemoveListener(ExitGame);
        
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        pc.ExitUIMode();
    }

    protected override void OnOpen()
    {
        if (SceneManager.GetActiveScene().name == SceneName.Loading)
        {
            UIController.Instance.CloseAsync<UI_Pause>().Forget();
            return;
        }
        
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

    protected override UniTask OnOpenAnimation()
    {
        _ = _popupDoTween.PlayOpenAsync();
        return base.OnOpenAnimation();
    }

    protected override UniTask OnCloseAnimation()
    {
        _ = _popupDoTween.PlayCloseAsync();
        return base.OnCloseAnimation();
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
        // todo. 설정 UI 열기
    }

    private void ChangeSceneToLobby()
    {
        // todo. 책임 위치 변경(씬 변경 "요청")
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
        // todo. 책임 위치 변경(데이터 저장 및 게임 종료 "요청")
        Application.Quit();
    }
}
