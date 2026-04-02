using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonPortal : MonoBehaviour, INpcInteraction
{
    [SerializeField] private UI_DungeonPortal _ui;
    [SerializeField] private DungeonMapConfig[] _dungeonConfigs;
    [SerializeField] private float _gatherRadius = 10f;

    private PlayerController _playerController;
    private PlayerNPCInteractionAbility _playerInteraction;

    private void Awake()
    {
        if (_ui == null)
            _ui = FindFirstObjectByType<UI_DungeonPortal>();
    }

    public void RequestInteract(Transform interactor)
    {
        _playerController = interactor.GetComponentInParent<PlayerController>();
        _playerInteraction = interactor.GetComponentInChildren<PlayerNPCInteractionAbility>();

        _playerController?.EnterUIMode();
        _ui.ShowDungeonSelection(_dungeonConfigs, OnSelectDungeon, EndInteraction);
    }

    private void OnSelectDungeon(int floor)
    {
        var config = _dungeonConfigs[floor - 1];
        var inventory = _playerController.GetAbility<PlayerInventoryAbility>();

        _ui.ShowRequirements(
            config.EntryRequirements,
            item => inventory.GetItemCount(item),
            () => OnConfirmEnter(floor),
            () => _ui.ShowDungeonSelection(_dungeonConfigs, OnSelectDungeon, EndInteraction));
    }

    private void OnConfirmEnter(int floor)
    {
        if (!AreAllPlayersNearby())
        {
            _ui.SetDescription("모든 플레이어가 근처에 있어야 합니다.",
                () => _ui.ShowDungeonSelection(_dungeonConfigs, OnSelectDungeon, EndInteraction));
            return;
        }

        var config = _dungeonConfigs[floor - 1];
        var inventory = _playerController.GetAbility<PlayerInventoryAbility>();

        if (config.EntryRequirements != null && config.EntryRequirements.Length > 0)
        {
            foreach (var req in config.EntryRequirements)
            {
                if (inventory.GetItemCount(req.Item) < req.Amount)
                {
                    _ui.SetDescription("재료가 부족합니다.",
                        () => _ui.ShowDungeonSelection(_dungeonConfigs, OnSelectDungeon, EndInteraction));
                    return;
                }
            }

            foreach (var req in config.EntryRequirements)
                inventory.RemoveItem(req.Item, req.Amount);
        }

        EndInteraction();
        EnterDungeon(floor);
    }

    private void EnterDungeon(int floor)
    {
        if (PhotonNetwork.IsConnected)
        {
            // 마스터든 클라이언트든 MapSyncManager를 통해 요청
            // 마스터가 수신하면 저장 후 씬 전환
            if (MapSyncManager.Instance != null)
            {
                MapSyncManager.Instance.OnDungeonEntryRequested -= OnMasterReceiveEntry;
                MapSyncManager.Instance.OnDungeonEntryRequested += OnMasterReceiveEntry;
                MapSyncManager.Instance.RequestDungeonEntry(floor);
            }
        }
        else
        {
            DungeonSceneInit.FloorOverride = floor;
            SceneManager.LoadScene(SceneName.Dungeon1);
        }
    }

    private void OnMasterReceiveEntry(int floor)
    {
        if (MapSyncManager.Instance != null)
            MapSyncManager.Instance.OnDungeonEntryRequested -= OnMasterReceiveEntry;

        MasterEnterDungeon(floor).Forget();
    }

    private async UniTaskVoid MasterEnterDungeon(int floor)
    {
        DungeonSceneInit.FloorOverride = floor;

        if (SaveManager.Instance != null)
        {
            int slot = RoomManager.Instance != null ? RoomManager.Instance.SelectedSlot : 0;
            await SaveManager.Instance.SaveAsync(slot);
        }

        PhotonNetwork.LoadLevel(SceneName.Dungeon1);
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

    public void EndInteraction()
    {
        _ui.Close();
        _playerController?.ExitUIMode();
        _playerInteraction?.EndInteraction();
        _playerInteraction = null;
        _playerController = null;
    }
}