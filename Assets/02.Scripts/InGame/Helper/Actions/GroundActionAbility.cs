using System.Collections.Generic;
using DG.Tweening;
using Photon.Pun;
using UnityEngine;
using System;

public class GroundActionAbility : HelperAbility, IHelperAction
{
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
    [SerializeField] private float _absorbArcHeight = 2f; // 흡수할때 땅블록 튀는높이
    [SerializeField] private float _absorbDuration = 0.5f; // 흡수 시간
    [SerializeField] private float _absorbTargetScale = 0.05f;
    [SerializeField] private float _digAnimDelay = 0.2f;

    [Header("땅 생성 액션")]
    [SerializeField] private float _placeAnimLeadTime = 0.3f; // cell움직임 보다 먼저 애니메이션 실행
    [SerializeField] private float _placeHorizontalDuration = 0.1f;
    [SerializeField] private float _placeHoverDuration = 0.2f; //땅이 잠깐 뜨는 시간
    [SerializeField] private float _placeDropDuration = 0.15f;

    private HelperAnimationAbility _animAbility;
    private GroundSelectAbility _groundSelector;

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

    public void InteractPrimary(TerrainCell cell)
    {
        if (cell == null) return;
        if (!CanRemoveCell(cell)) return;

        bool isFarmLand = cell.Data.ObjectType == EGridObjectType.FarmLand;
        if (isFarmLand)
        {
            cell.Data.RemoveObject();
            cell.Refresh();
        }

        TerrainCell detachedCell = TerrainGridManager.Instance.TryDetachForAnimation(cell.GridPosition, _canDigLevel);
        if (detachedCell == null)
        {
            if (isFarmLand)
            {
                cell.Data.SetObject(EGridObjectType.FarmLand);
                cell.Refresh();
            }
            return;
        }

        _owner.BeginAction();

        AnimateCellToMouth(detachedCell);

        var pos = cell.GridPosition;
        if (isFarmLand)
        {
            _owner.PhotonView.RpcSafe(nameof(RPC_DigFarmLandWithAnimation), RpcTarget.Others, pos.x, pos.y, pos.z, _canDigLevel);
        }
        else
        {
            _owner.PhotonView.RpcSafe(nameof(RPC_DigWithAnimation), RpcTarget.Others, pos.x, pos.y, pos.z, _canDigLevel);
        }

        PlayerInventoryAbility inventory = GetInventory();
        if (inventory != null && _dirtItem != null)
        {
            inventory.AddItem(_dirtItem, _getDirtAmount);
        }

        _owner.EndAction();
    }

    public void InteractSecondary(TerrainCell cell)
    {
        if (cell == null) return;

        PlayerInventoryAbility inventory = GetInventory();
        if (inventory == null) return;

        ItemDataSO selectedGround = _groundSelector?.SelectedGround;
        int groundSlotIndex = _groundSelector?.SelectedGroundSlotIndex ?? -1;

        if (selectedGround == null || groundSlotIndex < 0)
        {
            Debug.Log("인벤토리에 땅 아이템 없음");
            return;
        }

        if (cell.Data.ObjectType == EGridObjectType.Tree || cell.Data.ObjectType == EGridObjectType.Rock)
            return;

        ETileType tileType = GetTileTypeForItem(selectedGround, cell.Data.TileType);
        Vector3Int targetGridPos = GetPlacePosition(cell);

        bool placed = TerrainGridManager.Instance.TryPlaceBlock(targetGridPos, tileType, _generateDirtAmount);
        if (!placed) return;

        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.EatGround);

        TerrainCell newCell = TerrainGridManager.Instance.GetCell(targetGridPos);
        if (newCell != null && _mouthPoint != null)
        {
            Vector3 targetWorldPos = TerrainGridManager.Instance.GridToWorld(targetGridPos);
            StartPlaceCellAnimation(newCell, targetWorldPos);
        }

        _owner.PhotonView.RpcSafe(
            nameof(RPC_PlaceBlockWithAnimation), RpcTarget.Others,
            targetGridPos.x, targetGridPos.y, targetGridPos.z, (int)tileType, _generateDirtAmount);

        inventory.RemoveAt(groundSlotIndex, _generateDirtAmount);

        _owner.EndAction();
    }

    private bool CanRemoveCell(TerrainCell cell)
    {
        if (cell.Data.TileType == ETileType.Dungeon2Lava) return false;

        if (cell.Data.ObjectType == EGridObjectType.FarmLand)
        {
            FarmTile farmTile = cell.FarmTile;
            if (farmTile != null && farmTile.HasSeed) return false;
            return cell.Data.CanDig(_canDigLevel);
        }

        return cell.Data.CanDig(_canDigLevel);
    }

    private void AnimateCellToMouth(TerrainCell cell)
    {
        if (_mouthPoint == null)
        {
            Destroy(cell.gameObject);
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
                          _animAbility?.Play(EHelperAnim.Idle);
                      });

        cell.transform.DOScale(_absorbTargetScale, _absorbDuration)
                      .SetEase(Ease.Linear);

        DOVirtual.DelayedCall(_digAnimDelay, () => _animAbility?.Play(EHelperAnim.EatGround));
    }

    private void StartPlaceCellAnimation(TerrainCell cell, Vector3 targetWorldPos)
    {
        cell.transform.position = _mouthPoint.position;
        cell.transform.localScale = Vector3.zero;

        DOVirtual.DelayedCall(_placeAnimLeadTime, () => AnimatePlaceCell(cell, targetWorldPos));
    }

    private void AnimatePlaceCell(TerrainCell cell, Vector3 targetWorldPos)
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
            });
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

    private Vector3Int GetPlacePosition(TerrainCell cell)
    {
        if (cell.Data.CellType == ECellType.Empty)
            return cell.GridPosition;

        return cell.GridPosition + Vector3Int.up;
    }

    private void OnDisable()
    {
        _owner?.EndAction();
    }

    [PunRPC]
    internal void RPC_DigWithAnimation(int gridX, int gridY, int gridZ, int toolLevel)
    {
        TerrainCell detachedCell = TerrainGridManager.Instance?.TryDetachForAnimation(
            new Vector3Int(gridX, gridY, gridZ), toolLevel);
        if (detachedCell == null) return;

        AnimateCellToMouth(detachedCell);
    }

    [PunRPC]
    internal void RPC_DigFarmLandWithAnimation(int gridX, int gridY, int gridZ, int toolLevel)
    {
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

        AnimateCellToMouth(detachedCell);
    }

    [PunRPC]
    internal void RPC_PlaceBlockWithAnimation(int gridX, int gridY, int gridZ, int tileType, int dirtLevel)
    {
        var targetGridPos = new Vector3Int(gridX, gridY, gridZ);
        bool placed = TerrainGridManager.Instance.TryPlaceBlock(targetGridPos, (ETileType)tileType, dirtLevel);
        if (!placed) return;

        _animAbility?.Play(EHelperAnim.EatGround);

        TerrainCell newCell = TerrainGridManager.Instance.GetCell(targetGridPos);
        if (newCell != null && _mouthPoint != null)
        {
            Vector3 targetWorldPos = TerrainGridManager.Instance.GridToWorld(targetGridPos);
            StartPlaceCellAnimation(newCell, targetWorldPos);
        }
    }
}
