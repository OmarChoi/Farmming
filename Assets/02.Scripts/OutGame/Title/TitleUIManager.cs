using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

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

    [Header("방 참여")]
    [SerializeField] private TMP_InputField _roomIdInput;
    [SerializeField] private TMP_Text _joinErrorText;

    [Header("방 생성 완료")]
    [SerializeField] private TMP_Text _createdRoomIdText;

    [Header("참조")]
    [SerializeField] private TitleFlowManager _flowManager;

    private void Awake()
    {
        
        _flowManager.OnPanelChanged += ShowPanel;
        _flowManager.OnRoomIdReady += roomId => _createdRoomIdText.text = roomId;
        _flowManager.OnJoinError += msg => { if (_joinErrorText != null) _joinErrorText.text = msg; };
    }

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        ShowPanel(ETitlePanel.Title);
    }

    private void OnDestroy()
    {
        if (_flowManager != null)
            _flowManager.OnPanelChanged -= ShowPanel;
    }

    // === Button Handlers (Inspector 연결) ===

    public void OnClickGameStart()
    {
        ShowPanel(ETitlePanel.Lobby);
        RefreshSlotUI().Forget();
    }

    public void OnClickCreateRoom() => _flowManager.CreateNewGame();
    public void OnClickRoomStart() => _flowManager.StartRoom();

    public void OnClickJoinRoom()
    {
        ShowPanel(ETitlePanel.Join);
        _roomIdInput.text = "";
        if (_joinErrorText != null) _joinErrorText.text = "";
    }

    public void OnClickConfirmJoin() => _flowManager.ConfirmJoin(_roomIdInput.text.Trim().ToUpper());
    public void OnClickJoinBack() => ShowPanel(ETitlePanel.Lobby);
    public void OnClickRoomBack() => _flowManager.OnClickRoomBack();
    public void OnClickLobbyBack() => ShowPanel(ETitlePanel.Title);

    // === Slot UI ===

    private async UniTaskVoid RefreshSlotUI()
    {
        foreach (Transform child in _saveSlotParent)
            Destroy(child.gameObject);

        await _flowManager.RefreshSlotsAsync();

        var service = _flowManager.SlotService;
        for (int i = 0; i < service.MaxSlots; i++)
        {
            var slot = Instantiate(_saveSlotPrefab, _saveSlotParent);
            int idx = i;
            bool hasSave = service.HasSave(i);

            if (hasSave)
                slot.Setup(i, true,
                    () => _flowManager.LoadExistingGame(idx),
                    () => DeleteSlot(idx));
            else
                slot.Setup(i, false, null, null);
        }
    }

    private void DeleteSlot(int slot)
    {
        _flowManager.DeleteSlotAsync(slot).ContinueWith(() => RefreshSlotUI().Forget());
    }

    // === Panel ===

    private void ShowPanel(ETitlePanel panel)
    {
        _titlePanel.SetActive(panel == ETitlePanel.Title);
        _lobbyPanel.SetActive(panel == ETitlePanel.Lobby);
        _joinPanel.SetActive(panel == ETitlePanel.Join);
        _roomPanel.SetActive(panel == ETitlePanel.Room);
        _connectingPanel.SetActive(panel == ETitlePanel.Connecting);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
