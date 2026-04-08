using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 수확 공룡: IsHarvestable -> 수확
public class HarvestActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private HarvestItemSO _harvestItem;
    [SerializeField] private int _harvestExperience = 10;
    [SerializeField] private GameObject _normalVfxPrefab;
    [SerializeField] private float _normalVfxSpawnHeight = 2.1f;
    [SerializeField] private float _normalVfxLifetime = 2f;
    [SerializeField] private float _harvestDelay = 1.5f;


    private HelperAnimationAbility _animAbility;
    private HarvestEpicVFXAbility _epicVFX;
    private HarvestLegendaryVFXAbility _legendaryVFX;

    protected override void Awake()
    {
        base.Awake();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
        _epicVFX = _owner.GetAbility<HarvestEpicVFXAbility>();
        _legendaryVFX = _owner.GetAbility<HarvestLegendaryVFXAbility>();
    }

    private PlayerInventoryAbility GetInventory()
    {
        return _owner.PlayerOwner?.GetAbility<PlayerInventoryAbility>();
    }

    private void OnDisable()
    {
        _owner?.EndAction();
    }

    public bool CanInteractPrimary(TerrainCell cell)
    {
        return IsHarvestableCell(cell);
    }

    public void InteractPrimary(TerrainCell cell)
    {
        switch (_owner.Grade.CurrentGrade)
        {
            case EHelperGrade.Normal:
                InteractPrimaryNormal(cell);
                break;
            case EHelperGrade.Epic:
                InteractPrimaryEpic(cell);
                break;
            case EHelperGrade.Legendary:
                InteractPrimaryLegendary(cell);
                break;
        }
    }

    private void InteractPrimaryNormal(TerrainCell cell)
    {
        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null) return;
        if (!farmTile.HasSeed) return;

        CropGrowth cropGrowth = farmTile.CropGrowth;
        if (cropGrowth == null) return;
        if (!cropGrowth.IsHarvestable) return;

        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.NormalHarvest);

        if (_normalVfxPrefab != null)
        {
            Vector3 spawnPos = cell.transform.position + Vector3.up * _normalVfxSpawnHeight;
            GameObject vfx = Instantiate(_normalVfxPrefab, spawnPos, Quaternion.identity);
            Destroy(vfx, _normalVfxLifetime);
        }

        StartCoroutine(HarvestAfterDelay(farmTile, _harvestDelay));
    }

    private IEnumerator HarvestAfterDelay(FarmTile farmTile, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (HarvestCell(farmTile, farmTile.PlantedSeed))
            _owner.Experience.Add(_harvestExperience);

        farmTile.Interact();
        _animAbility?.Play(EHelperAnim.Idle);
        _owner.EndAction();
    }

    private void InteractPrimaryEpic(TerrainCell centerCell)
    {
        _owner.BeginAction();

        bool anyHarvested = false;

        if (_epicVFX != null)
        {
            _epicVFX.SpawnEffects(centerCell, cell =>
            {
                if (TryHarvestCell(cell)) anyHarvested = true;
            }, () =>
            {
                if (anyHarvested) _owner.Experience.Add(_harvestExperience);
                _animAbility?.Play(EHelperAnim.Idle);
                _owner.EndAction();
            }, _ => ReplayEpicHarvest());
        }
        else
        {
            ReplayEpicHarvest();
            if (TryHarvestCell(centerCell)) anyHarvested = true;
            if (anyHarvested) _owner.Experience.Add(_harvestExperience);
            _animAbility?.Play(EHelperAnim.Idle);
            _owner.EndAction();
        }
    }

    private void ReplayEpicHarvest()
    {
        if (_animAbility == null) return;
        StartCoroutine(_animAbility.ForceReplayAndWait(EHelperAnim.EpicHarvest, 0f));
    }

    private void InteractPrimaryLegendary(TerrainCell centerCell)
    {
        List<TerrainCell> targetCells = GetLegendaryCells(centerCell);

        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.LegendaryHarvest);

        Vector3 rightDir = GetRightDirection();
        bool anyHarvested = false;

        var cellHarvests = new List<(TerrainCell, Action)>();
        foreach (TerrainCell targetCell in targetCells)
        {
            TerrainCell captured = targetCell;
            cellHarvests.Add((captured, () =>
            {
                if (TryHarvestCell(captured)) anyHarvested = true;
            }));
        }

        if (_legendaryVFX != null)
        {
            _legendaryVFX.SpawnEffects(centerCell, rightDir, cellHarvests, () =>
            {
                if (anyHarvested) _owner.Experience.Add(_harvestExperience);
                _animAbility?.Play(EHelperAnim.Idle);
                _owner.EndAction();
            });
        }
        else
        {
            foreach (var (_, harvest) in cellHarvests)
                harvest?.Invoke();
            if (anyHarvested) _owner.Experience.Add(_harvestExperience);
            _animAbility?.Play(EHelperAnim.Idle);
            _owner.EndAction();
        }
    }

    private bool TryHarvestCell(TerrainCell cell)
    {
        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null || !farmTile.HasSeed) return false;

        CropGrowth cropGrowth = farmTile.CropGrowth;
        if (cropGrowth == null || !cropGrowth.IsHarvestable) return false;

        bool success = HarvestCell(farmTile, farmTile.PlantedSeed);
        farmTile.Interact();
        return success;
    }

    private bool HarvestCell(FarmTile farmTile, SeedItemDataSO seed)
    {
        int harvestAmount = UnityEngine.Random.Range(seed.HarvestAmountMin, seed.HarvestAmountMax + 1);

        PlayerInventoryAbility inventory = GetInventory();
        bool success = false;
        if (inventory != null && seed.HarvestItem != null)
        {
            QuestReportItemHelper.AddItemAndReportQuest(inventory, seed.HarvestItem, harvestAmount);
            success = true;
        }

        if (seed.Icon != null)
        {
            _harvestItem?.Raise(seed.Icon, seed.DisplayName, harvestAmount);
        }

        return success;
    }

    private bool IsHarvestableCell(TerrainCell cell)
    {
        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null || !farmTile.HasSeed) return false;
        CropGrowth cropGrowth = farmTile.CropGrowth;
        return cropGrowth != null && cropGrowth.IsHarvestable;
    }

    private List<TerrainCell> GetLegendaryCells(TerrainCell centerCell)
    {
        var cells = new List<TerrainCell> { centerCell };

        Vector3Int rightOffset = GetGridRightOffset();
        for (int i = 1; i <= 2; i++)
        {
            var rightCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset * i);
            var leftCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition - rightOffset * i);

            if (rightCell != null && rightCell.Data.IsTop) cells.Add(rightCell);
            if (leftCell != null && leftCell.Data.IsTop) cells.Add(leftCell);
        }
        return cells;
    }

    private Vector3Int GetGridRightOffset()
    {
        if (_owner.PlayerOwner == null) return Vector3Int.right;
        Vector3 right = _owner.PlayerOwner.transform.right;
        return new Vector3Int(Mathf.RoundToInt(right.x), 0, Mathf.RoundToInt(right.z));
    }

    private Vector3 GetRightDirection()
    {
        if (_owner.PlayerOwner == null) return Vector3.right;
        return _owner.PlayerOwner.transform.right;
    }

    public void InteractSecondary(TerrainCell cell)
    {
        // TODO: 우클릭 동작 구현
        _owner.EndAction();
    }

    private FarmTile GetFarmTile(TerrainCell cell)
    {
        if (cell == null) return null;
        if (cell.FarmTile != null && cell.FarmTile.gameObject.activeSelf)
        {
            return cell.FarmTile;
        }
        return null;
    }
}
