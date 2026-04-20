using System;
using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BaseBuilding))]
public class DungeonPortal : MonoBehaviour, IWorldInteractable
{
    [SerializeField] private UI_DungeonPortal _ui;
    [SerializeField] private DungeonMapConfig[] _dungeonConfigs;
    [SerializeField] private float _gatherRadius = 10f;

    private PlayerController _playerController;
    private PlayerNPCInteractionAbility _playerInteraction;
    private BaseBuilding _building;
    
    private void Awake()
    {
        _building = GetComponent<BaseBuilding>();
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
        if (!_building.IsConstructionComplete) return;
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
        _ui?.Close();
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
    
    public string AnimationTrigger => string.Empty;
}
