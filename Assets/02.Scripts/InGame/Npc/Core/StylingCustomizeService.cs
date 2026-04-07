using UnityEngine;
using Cysharp.Threading.Tasks;
using Photon.Pun;

public class StylingCustomizeService : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CharacterCustomizeUI _customizeUI;

    private NpcInteractionContext _currentContext;
    private PlayerController _localPlayer;
    private PlayerCustomizeAbility _customizeAbility;
    private CustomizeSaveData _originalData;
    private bool _isStyling;

    private void OnEnable()
    {
        if (_customizeUI != null)
        {
            _customizeUI.OnConfirmRequested += HandleConfirmRequested;
            _customizeUI.OnCancelRequested += HandleCancelRequested;
        }
    }

    private void OnDisable()
    {
        if (_customizeUI != null)
        {
            _customizeUI.OnConfirmRequested -= HandleConfirmRequested;
            _customizeUI.OnCancelRequested -= HandleCancelRequested;
        }
    }

    public void BeginStylingInteraction(NpcInteractionContext context)
    {
        _currentContext = context;
        BeginStyling();
    }

    public void BeginStyling()
    {
        if (_isStyling) return;

        _localPlayer = FindLocalPlayer();
        if (_localPlayer == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("스타일링 실패: 로컬 플레이어를 찾을 수 없습니다.");
#endif
            return;
        }

        _customizeAbility = _localPlayer.GetAbility<PlayerCustomizeAbility>();
        if (_customizeAbility == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("스타일링 실패: PlayerCustomizeAbility를 찾을 수 없습니다.");
#endif
            return;
        }

        CharacterPartSwapper swapper = _customizeAbility.PartSwapper;
        if (swapper == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("스타일링 실패: CharacterPartSwapper를 찾을 수 없습니다.");
#endif
            return;
        }

        _originalData = _customizeAbility.GetCurrentData();
        _isStyling = true;

        _localPlayer.EnterUIMode();
        _localPlayer.LockAction();

        _customizeUI.OpenForStyling(swapper, _originalData);
    }

    private async void HandleConfirmRequested(CustomizeSaveData finalData)
    {
        await ConfirmAsync(finalData);
    }

    private void HandleCancelRequested()
    {
        Cancel();
    }

    public async UniTask ConfirmAsync(CustomizeSaveData finalData)
    {
        if (!_isStyling || _customizeAbility == null) return;

        if (finalData == null) finalData = _customizeAbility.GetCurrentData();

        _customizeAbility.Initialize(finalData);

        if (CustomizeData.Instance != null)
        {
            CustomizeData.Instance.SetData(finalData);
        }

        await TrySaveAsync();

        EndStyling();
    }

    public void Cancel()
    {
        if (!_isStyling || _customizeAbility == null) return;

        _customizeAbility.Initialize(_originalData);
        EndStyling();
    }

    private void EndStyling()
    {
        if (_customizeUI != null)
        {
            _customizeUI.CloseForStyling();
        }

        if (_localPlayer != null)
        {
            _localPlayer.UnlockAction();
            _localPlayer.ExitUIMode();
        }
        _currentContext?.InteractionComponent?.EndInteraction();
        _localPlayer = null;
        _customizeAbility = null;
        _originalData = null;
        _isStyling = false;
    }

    private async UniTask TrySaveAsync()
    {
        if (SaveManager.Instance == null) return;

        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
        {
#if UNITY_EDITOR
            Debug.Log("비마스터 클라이언트이므로 즉시 저장은 건너뜁니다.");
#endif
            return;
        }

        int slotIndex = 0;
        if (RoomManager.Instance != null)
        {
            slotIndex = RoomManager.Instance.SelectedSlot;
        }

        await SaveManager.Instance.SaveAsync(slotIndex);
    }

    private PlayerController FindLocalPlayer()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController player in players)
        {
            if (player != null && player.IsMine) return player;
        }

        return null;
    }
}