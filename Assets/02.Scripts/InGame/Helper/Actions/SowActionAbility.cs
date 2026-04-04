using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class SowActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private GameObject _seedVfxPrefab;
    [SerializeField] private float _sowDelay = 0.5f;

    [SerializeField] private int _cultivateExperience = 10;
    [SerializeField] private int _sowExperience = 10;

    private CultivateAbility _cultivateAbility;
    private SeedSelectAbility _seedSelector;
    private HelperAnimationAbility _animAbility;

    private bool _isActing = false;
    private List<FarmTile> _currentFarmTiles;
    private SeedItemDataSO _currentSeed;

    private void Start()
    {
        _cultivateAbility = _owner.GetAbility<CultivateAbility>();
        _seedSelector = _owner.GetAbility<SeedSelectAbility>();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
    }

    public void InteractPrimary(TerrainCell cell)
    {
        if (cell == null)
        {
            return;
        }

        bool isEpic      = _owner.Grade.CurrentGrade == EHelperGrade.Epic;
        bool isLegendary = _owner.Grade.CurrentGrade == EHelperGrade.Legendary;

        System.Action onEpicLeft  = isEpic ? () => TryConvertLateralCell(cell, -1) : (System.Action)null;
        System.Action onEpicRight = isEpic ? () => TryConvertLateralCell(cell, +1) : (System.Action)null;

        if (cell.FarmTile == null || !cell.FarmTile.gameObject.activeSelf)
        {
            _owner.BeginAction();
            _cultivateAbility.JumpAndCultivate(cell,
                onCultivate: () =>
                {
                    cell.TryConvertToFarm();
                    _owner.Experience.Add(_cultivateExperience);
                    if (!isEpic)
                    {
                        foreach (TerrainCell lateralCell in GetLateralCells(cell))
                        {
                            if (lateralCell.FarmTile == null || !lateralCell.FarmTile.gameObject.activeSelf)
                                lateralCell.TryConvertToFarm();
                        }
                    }
                },
                spin: isLegendary, epicLook: isEpic,
                onEpicLookLeft: onEpicLeft, onEpicLookRight: onEpicRight);
            return;
        }

        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null)
        {
            return;
        }

        _owner.BeginAction();

        if (!farmTile.HasSeed)
        {
            _cultivateAbility.JumpAndCultivate(cell,
                onCultivate: isEpic ? (System.Action)null : () =>
                {
                    foreach (TerrainCell lateralCell in GetLateralCells(cell))
                    {
                        if (lateralCell.FarmTile == null || !lateralCell.FarmTile.gameObject.activeSelf)
                            lateralCell.TryConvertToFarm();
                    }
                },
                spin: isLegendary, epicLook: isEpic,
                onEpicLookLeft: onEpicLeft, onEpicLookRight: onEpicRight);
            return;
        }

        _cultivateAbility.JumpAndCultivate(cell, null,
            spin: isLegendary, epicLook: isEpic,
            onEpicLookLeft: onEpicLeft, onEpicLookRight: onEpicRight);
    }

    public void InteractSecondary(TerrainCell cell)
    {
        if (_owner.IsMine && _isActing)
        {
            return;
        }

        SeedItemDataSO selectedSeed = _seedSelector?.SelectedSeed;
        if(selectedSeed == null) return;

        List<FarmTile> farmTiles = GetSowableFarmTiles(cell);
        if(farmTiles.Count == 0) return;

        StartSow(farmTiles, selectedSeed);

        var pos = cell.GridPosition;
        _owner.PhotonView.RpcSafe(
            nameof(RPC_PlantSeed), RpcTarget.Others,
            pos.x, pos.y, pos.z, selectedSeed.Id);
    }

    private void StartSow(List<FarmTile> farmTiles, SeedItemDataSO seed)
    {
        _isActing = true;
        _currentFarmTiles = farmTiles;
        _currentSeed = seed;

        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.Sow);
    }

    public void SowOpen()
    {
        if (_currentFarmTiles == null || _currentFarmTiles.Count == 0 || _mouthPoint == null)
        {
            return;
        }

        Vector3 spawnPos = _mouthPoint.position;

        foreach(FarmTile tile in _currentFarmTiles)
        {
            FarmTile capturedTile = tile;
            Vector3 targetPos = capturedTile.CropSpawnPoint.position;
            Vector3 direction = (targetPos - spawnPos).normalized;

            if (_seedVfxPrefab != null)
            {
                GameObject vfxObj = Instantiate(_seedVfxPrefab, spawnPos, Quaternion.identity);
                SowVFX sowVfx = vfxObj.GetComponent<SowVFX>();
                sowVfx?.Launch(targetPos, direction);
            }

            StartCoroutine(PlantAfterDelay(capturedTile, _currentSeed));
        }
    }

    public void SowClose()
    {
        _animAbility?.Play(EHelperAnim.Idle);
        _currentFarmTiles = null;
        _currentSeed = null;
        _isActing = false;
        _owner.EndAction();
    }

    private IEnumerator PlantAfterDelay(FarmTile farmTile, SeedItemDataSO seed)
    {
        yield return new WaitForSeconds(_sowDelay);
        if (farmTile == null) yield break;

        farmTile.PlantSeed(seed);
        _owner.Experience.Add(_sowExperience);
    }

    private FarmTile GetFarmTile(TerrainCell cell)
    {
        if(cell == null)
        {
            return null;
        }
        if(cell.FarmTile != null && cell.FarmTile.gameObject.activeSelf)
        {
            return cell.FarmTile;
        }

        return null;
    }

    private void OnDisable()
    {
        _isActing = false;
        _currentFarmTiles = null;
        _currentSeed = null;
        _owner?.EndAction();
    }

    [PunRPC]
    internal void RPC_PlantSeed(int gridX, int gridY, int gridZ, int seedId)
    {
        var centerCell = TerrainGridManager.Instance?.GetCell(new Vector3Int(gridX, gridY, gridZ));
        if (centerCell == null) return;

        var seed = TerrainGridManager.Instance.SeedDatabase?.GetById(seedId);
        if (seed == null) return;

        List<FarmTile> farmTiles = GetSowableFarmTiles(centerCell);
        if (farmTiles.Count == 0) return;

        StartSow(farmTiles, seed);
    }

    private List<TerrainCell> GetLateralCells(TerrainCell centerCell)
    {
        var cells = new List<TerrainCell>();
        int extension = _owner.Grade.GetRange() - 1; // Normal:0  Epic:1
        if (extension <= 0)
        {
            return cells;
        }

        Vector3Int rightOffset = GetGridRightOffset();
        for (int i = 1; i <= extension; i++)
        {
            var rightCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset * i);
            var leftCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition - rightOffset * i);
            if (rightCell != null)
            {
                cells.Add(rightCell);
            }
            if (leftCell != null)
            {
                cells.Add(leftCell);
            }
        }
        return cells;
    }

    private List<TerrainCell> GetTargetCells(TerrainCell centerCell)
    {
        var cells = new List<TerrainCell> { centerCell };
        cells.AddRange(GetLateralCells(centerCell));
            return cells;
    }

    private List<FarmTile> GetSowableFarmTiles(TerrainCell centerCell)
    {
        var tiles = new List<FarmTile>();
        foreach(TerrainCell cell in GetTargetCells(centerCell))
        {
            FarmTile tile = GetFarmTile(cell);
            if(tile != null && tile.IsReadyToSow)
            {
                tiles.Add(tile);
            }
        }

        return tiles;
    }

    private void TryConvertLateralCell(TerrainCell centerCell, int directionSign)
    {
        Vector3Int rightOffset = GetGridRightOffset();
        TerrainCell lateralCell = TerrainGridManager.Instance?.GetCell(
            centerCell.GridPosition + rightOffset * directionSign);

        if (lateralCell != null && (lateralCell.FarmTile == null || !lateralCell.FarmTile.gameObject.activeSelf))
        {
            lateralCell.TryConvertToFarm();
        }
    }

    private Vector3Int GetGridRightOffset()
    {
        if (_owner.PlayerOwner == null)
        {
            return Vector3Int.right;
        }

        Vector3 right = _owner.PlayerOwner.transform.right;
        return new Vector3Int(Mathf.RoundToInt(right.x), 0, Mathf.RoundToInt(right.z));
    }
}