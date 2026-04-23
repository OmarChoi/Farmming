using System;
using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BaseBuilding))]
[RequireComponent(typeof(PhotonView))]
public class DungeonPortal : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private UI_DungeonPortal _ui;
    [SerializeField] private DungeonMapConfig[] _dungeonConfigs;
    [SerializeField] private float _gatherRadius = 10f;

    private PlayerController _playerController;
    private PlayerNPCInteractionAbility _playerInteraction;
    private BaseBuilding _building;
    private PhotonView _photonView;

    // 건설 완료된 경우에만 상호작용 가능.
    public bool CanInteract => _building != null && _building.IsConstructionComplete;

    private void Awake()
    {
        _building = GetComponent<BaseBuilding>();
        _photonView = GetComponent<PhotonView>();
        ResolveUI();
    }

    private void Start()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient && MapSyncManager.Instance != null)
            MapSyncManager.Instance.OnDungeonEntryRequested += OnMasterReceiveEntry;
    }

    private void OnDestroy()
    {
        if (MapSyncManager.Instance != null)
            MapSyncManager.Instance.OnDungeonEntryRequested -= OnMasterReceiveEntry;
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract) return;

        if (!ResolveUI())
        {
            Debug.LogError("[DungeonPortal] UI_DungeonPortal not found.");
            return;
        }

        _playerController = player;
        _playerInteraction = player.GetAbility<PlayerNPCInteractionAbility>();

        _playerController?.EnterUIMode();
        _ui.ShowDungeonSelection(_dungeonConfigs, OnSelectDungeon, EndInteract);
    }

    private void OnSelectDungeon(int floor)
    {
        var config = _dungeonConfigs[floor - 1];
        var inventory = _playerController.GetAbility<PlayerInventoryAbility>();
        int currentGold = CurrencyManager.Instance != null
            ? (int)CurrencyManager.Instance.GetGold()
            : 0;

        _ui.ShowRequirements(
            config.EntryRequirements,
            config.EntryCost,
            currentGold,
            item => inventory.GetItemCount(item),
            () => OnConfirmEnter(floor),
            () => _ui.ShowDungeonSelection(_dungeonConfigs, OnSelectDungeon, EndInteract));
    }

    private void OnConfirmEnter(int floor)
    {
        OnConfirmEnterAsync(floor).Forget();
    }

    private async UniTaskVoid OnConfirmEnterAsync(int floor)
    {
        if (!AreAllPlayersNearby())
        {
            _ui.SetDescription("모든 플레이어가 근처에 있어야 합니다.",
                () => _ui.ShowDungeonSelection(_dungeonConfigs, OnSelectDungeon, EndInteract));
            return;
        }

        ForceCancelLocalInteractions();

        var config = _dungeonConfigs[floor - 1];
        var inventory = _playerController.GetAbility<PlayerInventoryAbility>();
        Action backToSelection = () => _ui.ShowDungeonSelection(_dungeonConfigs, OnSelectDungeon, EndInteract);

        // 골드 체크
        if (config.EntryCost > 0 && (CurrencyManager.Instance == null || !CurrencyManager.Instance.CanAfford(config.EntryCost)))
        {
            _ui.SetDescription("골드가 부족합니다.", backToSelection);
            return;
        }

        // 재료 체크
        if (config.EntryRequirements != null && config.EntryRequirements.Length > 0)
        {
            foreach (var req in config.EntryRequirements)
            {
                if (inventory.GetItemCount(req.Item) < req.Amount)
                {
                    _ui.SetDescription("재료가 부족합니다.", backToSelection);
                    return;
                }
            }
        }

        // 골드 차감
        if (config.EntryCost > 0 && !CurrencyManager.Instance.TrySpendGold(config.EntryCost))
        {
            _ui.SetDescription("골드가 부족합니다.", backToSelection);
            return;
        }

        // 재료 차감
        if (config.EntryRequirements != null && config.EntryRequirements.Length > 0)
        {
            foreach (var req in config.EntryRequirements)
                inventory.RemoveItem(req.Item, req.Amount);
        }

        EndInteract();
        await EnterDungeon(floor);
    }

    private async UniTask EnterDungeon(int floor)
    {
        if (PhotonNetwork.IsConnected)
        {
            VillageCache.CapturePlayerPositions();
            if (TerrainGridManager.Instance != null)
                VillageCache.Capture(TerrainGridManager.Instance);

            if (MapSyncManager.Instance != null)
                MapSyncManager.Instance.RequestDungeonEntry(floor);
            return;
        }

        VillageCache.CapturePlayerPositions();
        if (TerrainGridManager.Instance != null)
            VillageCache.Capture(TerrainGridManager.Instance);

        // 모든 로컬 상호작용을 한 번 정리합니다.
        ForceCancelAllLocalInteractionsForTransition();

        if (SaveManager.Instance != null)
        {
            int slot = RoomManager.Instance != null ? RoomManager.Instance.SelectedSlot : 0;
            try
            {
                await SaveManager.Instance.SaveAsync(slot);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DungeonPortal] Failed to save before offline dungeon entry. {e}");
            }
        }

        DungeonSceneInit.FloorOverride = floor;
        SceneManager.LoadScene(SceneName.Dungeon1);
    }

    private void OnMasterReceiveEntry(int floor)
    {
        MasterEnterDungeon(floor).Forget();
    }

    private async UniTaskVoid MasterEnterDungeon(int floor)
    {
        DungeonSceneInit.FloorOverride = floor;

        // 모든 클라이언트에게 "지금 던전 전환 준비, 로컬 상호작용 종료"를 알립니다.
        if (_photonView != null)
        {
            _photonView.RPC(nameof(RPC_PrepareForDungeonTransition), RpcTarget.All);
        }

        // 각 클라이언트가 UI / 카메라 / 액션락 / 대화 상태를 정리할 시간을 조금 줍니다.
        // 정리를 넉넉하게 수행할 틈을 주기 위해 2번 넣었습니다.
        await UniTask.Yield();
        await UniTask.Yield();

        if (TerrainGridManager.Instance != null)
            VillageCache.Capture(TerrainGridManager.Instance);

        if (MapSyncManager.Instance != null)
            await MapSyncManager.Instance.PrepareVillageCacheForDungeonEntry();

        if (SaveManager.Instance != null)
        {
            int slot = RoomManager.Instance != null ? RoomManager.Instance.SelectedSlot : 0;
            await SaveManager.Instance.SaveAsync(slot);
        }

        SceneTransitionData.Type = ETransitionType.VillageToDungeon;
        SceneTransitionData.DungeonFloor = floor;

        if (PhotonNetwork.CurrentRoom != null)
        {
            var roomProps = new Hashtable
            {
                { SceneTransitionRoomProps.TransitionType, (int)ETransitionType.VillageToDungeon },
                { SceneTransitionRoomProps.DungeonFloor, floor }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        }

        PhotonNetwork.LoadLevel(SceneName.Loading);
    }

    [PunRPC]
    private void RPC_PrepareForDungeonTransition()
    {
        ForceCancelAllLocalInteractionsForTransition();
    }

    private bool AreAllPlayersNearby()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players.Length == 0) return false;

        foreach (var player in players)
        {
            float dist = Vector3.Distance(player.transform.position, transform.position);
            if (dist > _gatherRadius) return false;
        }

        return true;
    }

    public void EndInteract()
    {
        if (_ui != null)
        {
            _ui.Close();
        }
        _playerController?.ExitUIMode();
        _playerInteraction?.EndInteraction();
        _playerInteraction = null;
        _playerController = null;
    }

    private bool ResolveUI()
    {
        if (_ui != null)
            return true;

        _ui = FindFirstObjectByType<UI_DungeonPortal>(FindObjectsInactive.Include);
        return _ui != null;
    }

    private void ForceCancelAllLocalInteractionsForTransition()
    {
        PlayerController localPlayer = FindLocalPlayer();
        if (localPlayer == null) return;

        // 플레이어가 UI 모드에 들어가 있었다면 먼저 해제합니다.
        localPlayer.ExitUIMode();

        // NPC 대화/카메라/행동락을 강제 종료합니다.
        var npcInteraction = localPlayer.GetAbility<PlayerNPCInteractionAbility>();
        npcInteraction?.ForceCancelCurrentInteraction();

        // 혹시 포탈 UI가 열려 있다면 닫아 줍니다.
        _ui?.Close();
    }

    private void ForceCancelLocalInteractions()
    {
        var localPlayer = _playerController;
        if (localPlayer == null) return;

        var npcInteraction = localPlayer.GetAbility<PlayerNPCInteractionAbility>();
        npcInteraction?.ForceCancelCurrentInteraction();
    }

    private PlayerController FindLocalPlayer()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player != null && player.IsMine) return player;
        }

        return null;
    }

    public string AnimationTrigger => string.Empty;
}
