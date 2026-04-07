using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Photon.Pun;

public class StylingCustomizeService : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CharacterCustomizeUI _customizeUI;

    [Header("Camera")]
    [SerializeField] private CameraPreset _stylingCameraPreset;

    private NpcInteractionContext _currentContext;
    private PlayerController _localPlayer;
    private PlayerCustomizeAbility _customizeAbility;
    private CustomizeSaveData _originalData;
    private PlayerCameraAbility _cameraAbility;
    private bool _isStyling;

    private readonly List<Renderer> _hiddenRenderers = new();
    private readonly List<Collider> _hiddenColliders = new();

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
        _cameraAbility = _localPlayer.GetAbility<PlayerCameraAbility>();
        _originalData = _customizeAbility.GetCurrentData();
        _isStyling = true;

        _localPlayer.EnterUIMode();
        _localPlayer.LockAction();
        if (_cameraAbility != null && _stylingCameraPreset != null)
        {
            _cameraAbility.SnapYawToPlayerFrontView();
            _cameraAbility.SetPreset(_stylingCameraPreset);
        }

        HideStylingTargets();
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

        RestoreHiddenTargets();

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
    private void HideStylingTargets()
    {
        if (_currentContext == null) return;

        if (_currentContext.Npc != null)
        {
            HideTarget(_currentContext.Npc.gameObject);
        }

        GameObject buildingHideTarget = FindBuildingHideTargetFromContext();
        if (buildingHideTarget != null)
        {
            HideTarget(buildingHideTarget);
        }
    }

    private GameObject FindBuildingHideTargetFromContext()
    {
        if (_currentContext == null) return null;
        if (string.IsNullOrEmpty(_currentContext.NpcId)) return null;
        if (NpcLocationManager.Instance == null) return null;

        if (NpcLocationManager.Instance.TryGetFirstAnchor(_currentContext.NpcId, out NpcLocationAnchor anchor))
        {
            if (anchor != null)
            {
                return anchor.StylingHideRoot;
            }
        }

        return null;
    }

    private void HideTarget(GameObject target)
    {
        if (target == null) return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r == null || !r.enabled) continue;
            r.enabled = false;
            _hiddenRenderers.Add(r);
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        foreach (Collider c in colliders)
        {
            if (c == null || !c.enabled) continue;
            c.enabled = false;
            _hiddenColliders.Add(c);
        }
    }

    private void RestoreHiddenTargets()
    {
        foreach (Renderer r in _hiddenRenderers)
        {
            if (r != null)
            {
                r.enabled = true;
            }
        }
        _hiddenRenderers.Clear();

        foreach (Collider c in _hiddenColliders)
        {
            if (c != null)
            {
                c.enabled = true;
            }
        }
        _hiddenColliders.Clear();
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