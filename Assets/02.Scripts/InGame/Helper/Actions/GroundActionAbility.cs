using System.Collections.Generic;
using DG.Tweening;
using Photon.Pun;
using UnityEngine;
using System;

public class GroundActionAbility : HelperAbility, IHelperAction, ISecondaryInteractBlockNotifier
{
    private const string NoGroundItemMessage = "땅 아이템이 없습니다";

    [Serializable]
    private struct GroundItemMapping
    {
        public ItemDataSO Item;
        public ETileType TileType;
    }

    [Header("땅 제거")]
    [SerializeField] private ItemDataSO _dirtItem;
    [SerializeField] private int _canDigLevel = 1;
    [SerializeField] private int _getDirtAmount = 1;

    [Header("땅 생성")]
    [SerializeField] private int _generateDirtAmount = 1;
    [SerializeField] private List<GroundItemMapping> _groundItemMappings;

    [Header("땅제거 액션")]
    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private float _absorbArcHeight = 2f;  // 흡수할때 땅블록 튀는높이
    [SerializeField] private float _absorbDuration = 0.5f; // 흡수 시간
    [SerializeField] private float _absorbTargetScale = 0.05f;
    [SerializeField] private float _digAnimDelay = 0.2f;
    [SerializeField] private float _remoteDigStateSyncDelay = 0.05f;

    [Header("땅 생성 액션")]
    [SerializeField] private float _placeAnimLeadTime = 0.3f; // cell움직임 보다 먼저 애니메이션 실행
    [SerializeField] private float _placeHorizontalDuration = 0.1f;
    [SerializeField] private float _placeHoverDuration = 0.2f; //땅이 잠깐 뜨는 시간
    [SerializeField] private float _placeDropDuration = 0.15f;
    [SerializeField] private float _remotePlaceStateSyncDelay = 0.05f;

    [Header("Lava Melt")]
    [SerializeField] private GameObject _lavaMeltSmokePrefab;
    [SerializeField] private float _lavaMeltDuration = 2f;
    [SerializeField] private float _lavaMeltSmokeLifetime = 2.2f;
    [SerializeField] private float _lavaMeltStartDelayAfterPlacement = 0.2f;
    [SerializeField] private float _lavaMeltSfxFadeOutDuration = 0.25f;

    private HelperAnimationAbility _animAbility;
    private GroundSelectAbility _groundSelector;
    private bool _suppressSelectionBubble;
    private readonly HashSet<Vector3Int> _lavaMeltGridPositions = new HashSet<Vector3Int>();

    public bool ShouldShowSelectionBubble => !_suppressSelectionBubble;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        _animAbility = _owner.GetComponent<HelperAnimationAbility>();
        _groundSelector = _owner.GetAbility<GroundSelectAbility>();
    }

    private PlayerInventoryAbility GetInventory()
    {
        return _owner.PlayerOwner?.GetAbility<PlayerInventoryAbility>();
    }

    public bool CanUseGroundItem(ItemDataSO item)
    {
        if (item == null || _groundItemMappings == null)
            return false;

        // Ground items are the inventory items explicitly mapped to a placeable tile type.
        foreach (GroundItemMapping mapping in _groundItemMappings)
        {
            if (mapping.Item == item)
                return true;
        }

        return false;
    }

    public void InteractPrimary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return;
        if (PhotonNetwork.IsConnected && !_owner.IsMine) return;

        if (!CanRemoveCell(cell)) return;
        _groundSelector?.ClearSelection();
        SuppressSelectionBubble();

        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
        {
            Vector3Int pos = cell.GridPosition;

            bool isFarmLand = IsFarmLandCell(cell);
            TerrainCell detachedCell = DetachCellForPrimaryDig(cell, isFarmLand);
            if (detachedCell == null)
            {
                RestoreSelectionBubble();
                return;
            }

            StartPrimaryDigAction();
            PlayGroundDigSfx(detachedCell.transform.position);
            PlayPrimaryDigAnimation(detachedCell);
            FinishPrimaryDigAction();

            _owner.PhotonView.RpcSafe(
                nameof(RPC_RequestDigPrimary),
                RpcTarget.MasterClient,
                pos.x, pos.y, pos.z);
            _owner.EndAction();
            return;
        }

        ExecutePrimaryDig(cell, grantLocalReward: true, rewardTarget: null, broadcastAnimationToOthers: PhotonNetwork.IsConnected);
    }

    public void InteractSecondary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return;
        if (!CanPlaceGroundOnCell(cell)) return;

        PlayerInventoryAbility inventory = GetInventory();
        if (inventory == null) return;

        if (!TryResolveGroundSelection(inventory, showNoGroundMessage: true, out ItemDataSO selectedGround, out int groundSlotIndex))
            return;

        if (selectedGround == null || groundSlotIndex < 0)
        {
            Debug.Log("인벤토리에 땅 아이템 없음");
            return;
        }

        if (!HasSelectedGroundAmount(inventory, selectedGround, groundSlotIndex, _generateDirtAmount))
            return;

        if (cell.Data.ObjectType != EGridObjectType.None && cell.Data.ObjectType != EGridObjectType.FarmLand)
            return;

        ETileType tileType = GetTileTypeForItem(selectedGround, cell.Data.TileType);
        Vector3Int targetGridPos = GetPlacePosition(cell);
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
        {
            _owner.PhotonView.RpcSafe(
                nameof(RPC_RequestPlaceSecondary),
                RpcTarget.MasterClient,
                targetGridPos.x, targetGridPos.y, targetGridPos.z, (int)tileType, _generateDirtAmount);
            return;
        }

        ExecuteSecondaryPlace(
            targetGridPos,
            tileType,
            _generateDirtAmount,
            consumeLocalInventory: true,
            consumeTarget: null);
    }

    private void ExecuteSecondaryPlace(
        Vector3Int targetGridPos,
        ETileType tileType,
        int dirtAmount,
        bool consumeLocalInventory,
        Photon.Realtime.Player consumeTarget
    )
    {
        bool placed = TerrainGridManager.Instance.TryPlaceBlock(targetGridPos, tileType, dirtAmount);
        if (!placed) return;

        bool meltsOnLava = ShouldMeltPlacedGroundOnLava(tileType, targetGridPos);

        ClearFarmLandIfCovered(targetGridPos);

        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.EatGround);

        TerrainCell newCell = TerrainGridManager.Instance.GetCell(targetGridPos);
        Vector3 placedWorldPosition = newCell != null
            ? newCell.transform.position
            : TerrainGridManager.Instance.GridToWorld(targetGridPos);
        PlayGroundGenerateSfx(placedWorldPosition);

        if (newCell != null && _mouthPoint != null)
        {
            Vector3 targetWorldPos = TerrainGridManager.Instance.GridToWorld(targetGridPos);
            StartPlaceCellAnimation(newCell, targetWorldPos, meltsOnLava);
        }
        else if (meltsOnLava)
        {
            DOVirtual.DelayedCall(GetRemotePlaceAnimationDuration(), () => StartLavaMeltSequence(targetGridPos))
                     .SetTarget(gameObject);
        }

        _owner.PhotonView.RpcSafe(
            nameof(RPC_PlaceBlockWithAnimation), RpcTarget.Others,
            targetGridPos.x, targetGridPos.y, targetGridPos.z, (int)tileType, dirtAmount);

        if (PhotonNetwork.IsConnected)
        {
            float stateSyncDelay = meltsOnLava
                ? GetLavaMeltStateSyncDelay()
                : Mathf.Max(GetRemotePlaceAnimationDuration(), _remotePlaceStateSyncDelay);

            DOVirtual.DelayedCall(
                         stateSyncDelay,
                         () => BroadcastGroundStateFromMaster(targetGridPos))
                     .SetTarget(gameObject);
        }
        else
        {
            BroadcastGroundStateFromMaster(targetGridPos);
        }

        HandleSecondaryPlaceConsumption(tileType, dirtAmount, consumeLocalInventory, consumeTarget);

        _owner.EndAction();
    }

    private void ClearFarmLandIfCovered(Vector3Int placedGridPos)
    {
        TerrainCell belowCell = TerrainGridManager.Instance?.GetCell(placedGridPos + Vector3Int.down);
        if (belowCell == null || belowCell.Data == null)
            return;
        if (belowCell.Data.ObjectType != EGridObjectType.FarmLand)
            return;
        if (placedGridPos != belowCell.GridPosition + Vector3Int.up)
            return;

        FarmTile farmTile = belowCell.FarmTile;
        if (farmTile != null && (farmTile.HasSeed || farmTile.HasCrop))
            return;

        belowCell.Data.RemoveObject();
        belowCell.Refresh();
    }

    public bool CanInteractPrimary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return false;
        return CanRemoveCell(cell);
    }

    public bool CanInteractSecondary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return false;
        if (!CanPlaceGroundOnCell(cell)) return false;

        PlayerInventoryAbility inventory = GetInventory();
        if (inventory == null) return false;

        ItemDataSO selectedGround = _groundSelector?.SelectedGround;
        int groundSlotIndex = _groundSelector?.SelectedGroundSlotIndex ?? -1;
        return HasSelectedGroundAmount(inventory, selectedGround, groundSlotIndex, _generateDirtAmount)
               || HasAnyGroundAmount(inventory, _generateDirtAmount);
    }

    public void NotifySecondaryInteractBlocked(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return;
        if (!CanPlaceGroundOnCell(cell)) return;

        PlayerInventoryAbility inventory = GetInventory();
        if (inventory == null) return;

        ItemDataSO selectedGround = _groundSelector?.SelectedGround;
        int groundSlotIndex = _groundSelector?.SelectedGroundSlotIndex ?? -1;
        if (HasSelectedGroundAmount(inventory, selectedGround, groundSlotIndex, _generateDirtAmount))
            return;

        if (HasAnyGroundAmount(inventory, _generateDirtAmount))
            return;

        ShowNoGroundItemMessage();
    }

    private bool CanPlaceGroundOnCell(TerrainCell cell)
    {
        if (cell == null) return false;
        if (cell.Data.ObjectType != EGridObjectType.None && cell.Data.ObjectType != EGridObjectType.FarmLand)
            return false;

        if (cell.Data.ObjectType != EGridObjectType.FarmLand)
            return true;

        FarmTile farmTile = cell.FarmTile;
        if (farmTile == null)
            return true;

        return !farmTile.HasSeed && !farmTile.HasCrop;
    }

    private bool HasSelectedGroundAmount(PlayerInventoryAbility inventory, ItemDataSO selectedGround, int slotIndex, int requiredAmount)
    {
        if (inventory == null || selectedGround == null || slotIndex < 0)
            return false;

        InventorySlot slot = inventory.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty)
            return false;
        if (slot.Item != selectedGround)
            return false;

        return inventory.GetItemCount(selectedGround) >= requiredAmount;
    }

    private bool TryResolveGroundSelection(
        PlayerInventoryAbility inventory,
        bool showNoGroundMessage,
        out ItemDataSO selectedGround,
        out int groundSlotIndex
    )
    {
        selectedGround = _groundSelector?.SelectedGround;
        groundSlotIndex = _groundSelector?.SelectedGroundSlotIndex ?? -1;

        if (HasSelectedGroundAmount(inventory, selectedGround, groundSlotIndex, _generateDirtAmount))
            return true;

        if (_groundSelector != null && _groundSelector.TrySelectFirstAvailableGround(_generateDirtAmount))
        {
            selectedGround = _groundSelector.SelectedGround;
            groundSlotIndex = _groundSelector.SelectedGroundSlotIndex;
            if (HasSelectedGroundAmount(inventory, selectedGround, groundSlotIndex, _generateDirtAmount))
                return true;
        }

        selectedGround = null;
        groundSlotIndex = -1;

        if (showNoGroundMessage)
            ShowNoGroundItemMessage();

        return false;
    }

    private bool HasAnyGroundAmount(PlayerInventoryAbility inventory, int requiredAmount)
    {
        if (inventory == null)
            return false;

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            InventorySlot slot = inventory.GetSlot(i);
            if (slot == null || slot.IsEmpty)
                continue;

            if (CanUseGroundItem(slot.Item) && inventory.GetItemCount(slot.Item) >= requiredAmount)
                return true;
        }

        return false;
    }

    private static void ShowNoGroundItemMessage()
    {
        if (HarvestNotificationManager.Instance != null)
        {
            HarvestNotificationManager.Instance.ShowMessage(NoGroundItemMessage);
            return;
        }

        Debug.Log(NoGroundItemMessage);
    }

    private bool ShouldMeltPlacedGroundOnLava(ETileType placedTileType, Vector3Int targetGridPos)
    {
        if (placedTileType == ETileType.Dungeon3LavaStone)
            return false;

        TerrainCell belowCell = TerrainGridManager.Instance?.GetCell(targetGridPos + Vector3Int.down);
        if (belowCell == null || belowCell.Data == null)
            return false;

        return belowCell.Data.TileType == ETileType.Dungeon3Lava;
    }

    private bool CanRemoveCell(TerrainCell cell)
    {
        if (cell.Data.TileType == ETileType.Dungeon3Lava) return false;

        if (cell.Data.ObjectType == EGridObjectType.FarmLand)
        {
            FarmTile farmTile = cell.FarmTile;
            if (farmTile != null && (farmTile.HasSeed || farmTile.HasCrop)) return false;
            return cell.Data.CanDig(_canDigLevel);
        }

        return cell.Data.CanDig(_canDigLevel);
    }

    private void ExecutePrimaryDig(
        TerrainCell cell,
        bool grantLocalReward,
        Photon.Realtime.Player rewardTarget,
        bool broadcastAnimationToOthers
    )
    {
        if (!CanExecutePrimaryDig(cell))
            return;

        ETileType removedTileType = GetRemovedTileType(cell);
        bool isFarmLand = IsFarmLandCell(cell);
        TerrainCell detachedCell = DetachCellForPrimaryDig(cell, isFarmLand);
        if (detachedCell == null)
            return;

        StartPrimaryDigAction();
        PlayGroundDigSfx(detachedCell.transform.position);
        PlayPrimaryDigAnimation(detachedCell);
        SyncPrimaryDigState(detachedCell.GridPosition, cell.GridPosition, isFarmLand, broadcastAnimationToOthers);
        HandlePrimaryDigRewards(removedTileType, grantLocalReward, rewardTarget);
        FinishPrimaryDigAction();
    }

    private bool CanExecutePrimaryDig(TerrainCell cell)
    {
        return cell != null && CanRemoveCell(cell);
    }

    private static ETileType GetRemovedTileType(TerrainCell cell)
    {
        return cell.Data.TileType;
    }

    private static bool IsFarmLandCell(TerrainCell cell)
    {
        return cell.Data.ObjectType == EGridObjectType.FarmLand;
    }

    private TerrainCell DetachCellForPrimaryDig(TerrainCell cell, bool isFarmLand)
    {
        PrepareFarmLandForPrimaryDig(cell, isFarmLand);

        TerrainCell detachedCell = TerrainGridManager.Instance.TryDetachForAnimation(cell.GridPosition, _canDigLevel);
        if (detachedCell == null)
            RestoreFarmLandAfterFailedDetach(cell, isFarmLand);

        return detachedCell;
    }

    private static void PrepareFarmLandForPrimaryDig(TerrainCell cell, bool isFarmLand)
    {
        if (!isFarmLand)
            return;

        cell.FarmTile?.ClearFastFertilizer();
        cell.Data.RemoveObject();
        cell.Refresh();
    }

    private static void RestoreFarmLandAfterFailedDetach(TerrainCell cell, bool isFarmLand)
    {
        if (!isFarmLand)
            return;

        cell.Data.SetObject(EGridObjectType.FarmLand);
        cell.Refresh();
    }

    private void StartPrimaryDigAction()
    {
        _owner.BeginAction();
    }

    private void PlayPrimaryDigAnimation(TerrainCell detachedCell)
    {
        AnimateCellToMouth(detachedCell);
    }

    private void SyncPrimaryDigState(
        Vector3Int detachedGridPosition,
        Vector3Int originalGridPosition,
        bool isFarmLand,
        bool broadcastAnimationToOthers
    )
    {
        if (broadcastAnimationToOthers)
        {
            BroadcastDigAnimation(originalGridPosition, isFarmLand);
            DOVirtual.DelayedCall(
                         Mathf.Max(0f, _remoteDigStateSyncDelay),
                         () => BroadcastGroundStateFromMaster(detachedGridPosition))
                     .SetTarget(gameObject);
            return;
        }

        BroadcastGroundStateFromMaster(detachedGridPosition);
    }

    private void HandlePrimaryDigRewards(
        ETileType removedTileType,
        bool grantLocalReward,
        Photon.Realtime.Player rewardTarget
    )
    {
        if (grantLocalReward)
            GrantDigReward(removedTileType, _getDirtAmount);

        if (rewardTarget != null)
            GrantDigRewardToRemotePlayer(removedTileType, rewardTarget);
    }

    private void HandleSecondaryPlaceConsumption(
        ETileType placedTileType,
        int amount,
        bool consumeLocalInventory,
        Photon.Realtime.Player consumeTarget
    )
    {
        if (consumeLocalInventory)
            ConsumePlacedGroundItem(placedTileType, amount);

        if (consumeTarget != null)
        {
            _owner.PhotonView.RPC(
                nameof(RPC_ConsumePlacedGroundItem),
                consumeTarget,
                (int)placedTileType,
                amount);
        }
    }

    private void GrantDigRewardToRemotePlayer(ETileType removedTileType, Photon.Realtime.Player rewardTarget)
    {
        _owner.PhotonView.RPC(
            nameof(RPC_GrantDigReward),
            rewardTarget,
            (int)removedTileType,
            _getDirtAmount);
    }

    private void FinishPrimaryDigAction()
    {
        _owner.EndAction();
    }

    private void SuppressSelectionBubble()
    {
        _suppressSelectionBubble = true;
    }

    private void RestoreSelectionBubble()
    {
        _suppressSelectionBubble = false;
    }

    private void BroadcastDigAnimation(Vector3Int gridPosition, bool isFarmLand)
    {
        if (isFarmLand)
        {
            _owner.PhotonView.RpcSafe(
                nameof(RPC_DigFarmLandWithAnimation),
                RpcTarget.Others,
                gridPosition.x, gridPosition.y, gridPosition.z, _canDigLevel);
        }
        else
        {
            _owner.PhotonView.RpcSafe(
                nameof(RPC_DigWithAnimation),
                RpcTarget.Others,
                gridPosition.x, gridPosition.y, gridPosition.z, _canDigLevel);
        }
    }

    private void GrantDigReward(ETileType removedTileType, int amount)
    {
        PlayerInventoryAbility inventory = GetInventory();
        ItemDataSO rewardItem = GetRewardItemForTile(removedTileType);
        if (inventory == null || rewardItem == null || amount <= 0)
            return;

        inventory.AddItem(rewardItem, amount);
    }

    private void ConsumePlacedGroundItem(ETileType placedTileType, int amount)
    {
        if (amount <= 0)
            return;

        PlayerInventoryAbility inventory = GetInventory();
        ItemDataSO placedItem = GetRewardItemForTile(placedTileType);
        if (inventory == null || placedItem == null)
            return;

        inventory.RemoveItem(placedItem, amount);
    }

    private void AnimateCellToMouth(TerrainCell cell)
    {
        if (_mouthPoint == null)
        {
            Destroy(cell.gameObject);
            RestoreSelectionBubble();
            _animAbility?.Play(EHelperAnim.Idle);
            return;
        }

        cell.transform.SetParent(null);
        cell.transform.localScale = Vector3.one;

        cell.transform.DOJump(_mouthPoint.position, _absorbArcHeight, 1, _absorbDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                Destroy(cell.gameObject);
                RestoreSelectionBubble();
                _animAbility?.Play(EHelperAnim.Idle);
            });

        cell.transform.DOScale(_absorbTargetScale, _absorbDuration)
            .SetEase(Ease.Linear);

        DOVirtual.DelayedCall(_digAnimDelay, () => _animAbility?.Play(EHelperAnim.EatGround));
    }

    private void StartPlaceCellAnimation(TerrainCell cell, Vector3 targetWorldPos, bool meltAfterPlacement = false)
    {
        cell.transform.position = _mouthPoint.position;
        cell.transform.localScale = Vector3.zero;

        DOVirtual.DelayedCall(_placeAnimLeadTime, () => AnimatePlaceCell(cell, targetWorldPos, meltAfterPlacement))
                 .SetTarget(cell.gameObject);
    }

    private void AnimatePlaceCell(TerrainCell cell, Vector3 targetWorldPos, bool meltAfterPlacement)
    {
        if (_mouthPoint == null) return;

        Vector3 hoverPos = new Vector3(targetWorldPos.x, _mouthPoint.position.y, targetWorldPos.z);

        DOTween.Sequence()
               .Append(cell.transform.DOMove(hoverPos, _placeHorizontalDuration).SetEase(Ease.OutQuad))
               .Join(cell.transform.DOScale(Vector3.one * _absorbTargetScale, _placeHorizontalDuration).SetEase(Ease.OutQuad))
               .AppendInterval(_placeHoverDuration)
               .Append(cell.transform.DOMove(targetWorldPos, _placeDropDuration).SetEase(Ease.InExpo))
               .Join(cell.transform.DOScale(Vector3.one, _placeDropDuration).SetEase(Ease.OutExpo))
               .OnComplete(() =>
               {
                   cell.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 0.5f);
                   _animAbility?.Play(EHelperAnim.Idle);

                   if (meltAfterPlacement)
                       DOVirtual.DelayedCall(GetLavaMeltStartDelayAfterPlacement(), () => StartLavaMeltSequence(cell.GridPosition))
                                .SetTarget(cell.gameObject);
               });
    }

    private float GetRemotePlaceAnimationDuration()
    {
        return Mathf.Max(
            0f,
            _placeAnimLeadTime + _placeHorizontalDuration + _placeHoverDuration + _placeDropDuration);
    }

    private ETileType GetTileTypeForItem(ItemDataSO item, ETileType fallback)
    {
        if (_groundItemMappings == null) return fallback;

        foreach (GroundItemMapping mapping in _groundItemMappings)
        {
            if (mapping.Item == item)
                return mapping.TileType;
        }

        return fallback;
    }

    private ItemDataSO GetRewardItemForTile(ETileType tileType)
    {
        if (_groundItemMappings != null)
        {
            foreach (GroundItemMapping mapping in _groundItemMappings)
            {
                if (mapping.TileType == tileType && mapping.Item != null)
                    return mapping.Item;
            }
        }

        return _dirtItem;
    }

    private Vector3Int GetPlacePosition(TerrainCell cell)
    {
        if (cell.Data.CellType == ECellType.Empty)
            return cell.GridPosition;

        return cell.GridPosition + Vector3Int.up;
    }

    private static void BroadcastGroundStateFromMaster(Vector3Int changedCellPos)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        MapSyncManager.Instance?.BroadcastTerrainCellStatesFromMaster(
            changedCellPos,
            changedCellPos + Vector3Int.down);
    }

    private static void RestoreBelowCellTopAfterImmediateDestroy(Vector3Int removedGridPos)
    {
        TerrainCell belowCell = TerrainGridManager.Instance?.GetCell(removedGridPos + Vector3Int.down);
        if (belowCell == null || belowCell.Data == null)
            return;

        if (belowCell.Data.CellType != ECellType.Dirt)
            return;

        belowCell.Data.SetTop(true);
        belowCell.Refresh();
    }

    private void StartLavaMeltSequence(Vector3Int gridPos)
    {
        if (!_lavaMeltGridPositions.Add(gridPos))
            return;

        TerrainCell cell = TerrainGridManager.Instance?.GetCell(gridPos);
        if (cell == null)
        {
            _lavaMeltGridPositions.Remove(gridPos);
            return;
        }

        cell.transform.localScale = Vector3.one;

        SpawnLavaMeltSmoke(cell.transform.position);
        PlayLavaMeltSfx(cell.transform.position);

        float duration = Mathf.Max(0.01f, _lavaMeltDuration);
        cell.transform
            .DOScale(Vector3.zero, duration)
            .SetEase(Ease.InOutQuad)
            .SetTarget(cell.gameObject)
            .OnComplete(() =>
            {
                TerrainCell currentCell = TerrainGridManager.Instance?.GetCell(gridPos);
                if (currentCell == cell)
                {
                    TerrainGridManager.Instance.RemoveCell(gridPos);
                    RestoreBelowCellTopAfterImmediateDestroy(gridPos);
                }

                _lavaMeltGridPositions.Remove(gridPos);
            });
    }

    private void SpawnLavaMeltSmoke(Vector3 position)
    {
        if (_lavaMeltSmokePrefab == null)
            return;

        GameObject smoke = Instantiate(_lavaMeltSmokePrefab, position, Quaternion.identity);
        Destroy(smoke, Mathf.Max(_lavaMeltSmokeLifetime, _lavaMeltDuration));
    }

    private void PlayLavaMeltSfx(Vector3 position)
    {
        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.PlaySfxForDuration(
            new SfxPlayRequest(
                AssetKey.SFX.DirtSizzleLava,
                ESpatialMode.Positional3D,
                position),
            Mathf.Max(0.01f, _lavaMeltDuration),
            Mathf.Max(0f, _lavaMeltSfxFadeOutDuration));
    }

    private float GetLavaMeltStateSyncDelay()
    {
        return GetRemotePlaceAnimationDuration()
               + GetLavaMeltStartDelayAfterPlacement()
               + Mathf.Max(0.01f, _lavaMeltDuration)
               + Mathf.Max(0f, _remotePlaceStateSyncDelay);
    }

    private float GetLavaMeltStartDelayAfterPlacement()
    {
        return Mathf.Max(0f, _lavaMeltStartDelayAfterPlacement);
    }

    private void OnDisable()
    {
        RestoreSelectionBubble();
        _owner?.EndAction();
    }

    [PunRPC]
    internal void RPC_RequestDigPrimary(int gridX, int gridY, int gridZ, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        TerrainCell cell = TerrainGridManager.Instance?.GetCell(new Vector3Int(gridX, gridY, gridZ));
        cell = GetInteractableCell(cell);
        if (cell == null)
            return;

        ExecutePrimaryDig(
            cell,
            grantLocalReward: false,
            rewardTarget: info.Sender,
            broadcastAnimationToOthers: true);
    }

    [PunRPC]
    internal void RPC_RequestPlaceSecondary(int gridX, int gridY, int gridZ, int tileType, int dirtAmount, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Vector3Int targetGridPos = new Vector3Int(gridX, gridY, gridZ);
        if (!CanPlaceGroundAt(targetGridPos))
            return;

        ExecuteSecondaryPlace(
            targetGridPos,
            (ETileType)tileType,
            dirtAmount,
            consumeLocalInventory: false,
            consumeTarget: info.Sender);
    }

    [PunRPC]
    internal void RPC_GrantDigReward(int removedTileType, int amount)
    {
        GrantDigReward((ETileType)removedTileType, amount);
    }

    [PunRPC]
    internal void RPC_ConsumePlacedGroundItem(int placedTileType, int amount)
    {
        ConsumePlacedGroundItem((ETileType)placedTileType, amount);
    }

    [PunRPC]
    internal void RPC_DigWithAnimation(int gridX, int gridY, int gridZ, int toolLevel)
    {
        if (_owner.IsMine) return;
        TerrainCell detachedCell = TerrainGridManager.Instance?.TryDetachForAnimation(
            new Vector3Int(gridX, gridY, gridZ), toolLevel);
        if (detachedCell == null) return;

        PlayGroundDigSfx(detachedCell.transform.position);
        AnimateCellToMouth(detachedCell);
    }

    [PunRPC]
    internal void RPC_DigFarmLandWithAnimation(int gridX, int gridY, int gridZ, int toolLevel)
    {
        if (_owner.IsMine) return;
        var pos = new Vector3Int(gridX, gridY, gridZ);
        TerrainCell cell = TerrainGridManager.Instance?.GetCell(pos);
        if (cell == null) return;

        if (cell.Data.ObjectType == EGridObjectType.FarmLand)
        {
            cell.Data.RemoveObject();
            cell.Refresh();
        }

        TerrainCell detachedCell = TerrainGridManager.Instance.TryDetachForAnimation(pos, toolLevel);
        if (detachedCell == null) return;

        PlayGroundDigSfx(detachedCell.transform.position);
        AnimateCellToMouth(detachedCell);
    }

    [PunRPC]
    internal void RPC_PlaceBlockWithAnimation(int gridX, int gridY, int gridZ, int tileType, int dirtLevel)
    {
        var targetGridPos = new Vector3Int(gridX, gridY, gridZ);
        ETileType placedTileType = (ETileType)tileType;
        bool placed = TerrainGridManager.Instance.TryPlaceBlock(targetGridPos, placedTileType, dirtLevel);
        if (!placed) return;

        _animAbility?.Play(EHelperAnim.EatGround);

        TerrainCell newCell = TerrainGridManager.Instance.GetCell(targetGridPos);
        Vector3 placedWorldPosition = newCell != null
            ? newCell.transform.position
            : TerrainGridManager.Instance.GridToWorld(targetGridPos);
        PlayGroundGenerateSfx(placedWorldPosition);

        if (newCell != null && _mouthPoint != null)
        {
            Vector3 targetWorldPos = TerrainGridManager.Instance.GridToWorld(targetGridPos);
            StartPlaceCellAnimation(newCell, targetWorldPos, ShouldMeltPlacedGroundOnLava(placedTileType, targetGridPos));
        }
        else if (ShouldMeltPlacedGroundOnLava(placedTileType, targetGridPos))
        {
            DOVirtual.DelayedCall(GetRemotePlaceAnimationDuration(), () => StartLavaMeltSequence(targetGridPos))
                     .SetTarget(gameObject);
        }
    }

    private bool CanPlaceGroundAt(Vector3Int targetGridPos)
    {
        if (TerrainGridManager.Instance == null)
            return false;
        if (targetGridPos.y >= TerrainGridManager.Instance.MaxHeight)
            return false;

        TerrainCell targetCell = TerrainGridManager.Instance.GetCell(targetGridPos);
        if (targetCell != null && targetCell.Data != null && targetCell.Data.CellType != ECellType.Empty)
            return false;

        TerrainCell belowCell = TerrainGridManager.Instance.GetCell(targetGridPos + Vector3Int.down);
        if (belowCell == null || belowCell.Data == null)
            return false;

        if (!belowCell.Data.IsTop)
            return false;

        return CanPlaceGroundOnCell(belowCell);
    }

    private static void PlayGroundDigSfx(Vector3 position)
    {
        PlayGroundSfx(AssetKey.SFX.GroundDig, position);
    }

    private static void PlayGroundGenerateSfx(Vector3 position)
    {
        PlayGroundSfx(AssetKey.SFX.GroundGenerate, position);
    }

    private static void PlayGroundSfx(string clipKey, Vector3 position)
    {
        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.PlaySfx(new SfxPlayRequest(
            clipKey: clipKey,
            spatialMode: ESpatialMode.Positional3D,
            position: position));
    }
}
