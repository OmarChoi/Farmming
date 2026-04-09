using DG.Tweening;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CultivateAbility;

public class SowActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private GameObject _seedVfxPrefab;
    [SerializeField] private float _sowDelay = 0.5f;
    [SerializeField] private float _epicThreeTileLookDuration = 1.6f;

    [SerializeField] private int _cultivateExperience = 10;
    [SerializeField] private int _sowExperience = 10;

    [Header("Secondary - Float")]
    [SerializeField] private float _floatAbovePlayerHeight = 2f;
    [SerializeField] private float _floatMoveDuration = 0.5f;
    [SerializeField] private float _floatDuration = 2f;
    [SerializeField] private float _bobAmplitude = 0.3f;
    [SerializeField] private float _bobDuration = 0.4f;
    [SerializeField] private float _returnDuration = 0.5f;

    private CultivateAbility _cultivateAbility;
    private SeedSelectAbility _seedSelector;
    private HelperAnimationAbility _animAbility;
    private SowEpicSecondaryVFXAbility _epicSecondaryVFX;
    private SowLegendarySecondaryVFXAbility _legendarySecondaryVFX;

    private bool _isActing = false;
    private List<FarmTile> _currentFarmTiles;
    private SeedItemDataSO _currentSeed;

    private void Start()
    {
        _cultivateAbility = _owner.GetAbility<CultivateAbility>();
        _seedSelector = _owner.GetAbility<SeedSelectAbility>();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
        _epicSecondaryVFX = _owner.GetAbility<SowEpicSecondaryVFXAbility>();
        _legendarySecondaryVFX = _owner.GetAbility<SowLegendarySecondaryVFXAbility>();
    }

    public bool CanInteractPrimary(TerrainCell cell)
    {
        if (cell == null) return false;
        if (cell.Data.CellType != ECellType.Dirt) return false;
        if (cell.Data.ObjectType != EGridObjectType.None && cell.Data.ObjectType != EGridObjectType.FarmLand)
            return false;
        return true;
    }

    public bool CanInteractSecondary(TerrainCell cell)
    {
        if (cell == null) return false;
        if (_seedSelector == null || _seedSelector.SelectedSeed == null) return false;
        FarmTile farmTile = GetFarmTile(cell);
        return farmTile != null && farmTile.IsReadyToSow;
    }

    public void InteractPrimary(TerrainCell cell)
    {
        if (cell == null)
        {
            return;
        }

        switch (_owner.Grade.CurrentGrade)
        {
            case EHelperGrade.Epic:
                HandleEpicCultivation(cell);
                break;
            case EHelperGrade.Legendary:
                HandleLegendaryCultivation(cell);
                break;
            default:
                HandleNormalCultivation(cell);
                break;
        }
    }

    private void HandleNormalCultivation(TerrainCell cell)
    {
        if (cell.FarmTile == null || !cell.FarmTile.gameObject.activeSelf)
        {
            _owner.BeginAction();
            _cultivateAbility.JumpAndCultivate(cell, new CultivationParams
            {
                OnCultivate = () =>
                {
                    cell.TryConvertToFarm();
                    _owner.Experience.Add(_cultivateExperience);
                    foreach (TerrainCell lateralCell in GetLateralCells(cell))
                    {
                        if (lateralCell.FarmTile == null || !lateralCell.FarmTile.gameObject.activeSelf)
                        {
                            lateralCell.TryConvertToFarm();
                        }
                    }
                }
            });

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
            _cultivateAbility.JumpAndCultivate(cell, new CultivationParams
            {
                OnCultivate = () =>
                {
                    foreach (TerrainCell lateralCell in GetLateralCells(cell))
                    {
                        if (lateralCell.FarmTile == null || !lateralCell.FarmTile.gameObject.activeSelf)
                        {
                            lateralCell.TryConvertToFarm();
                        }
                    }
                }
            });
            return;
        }

        _cultivateAbility.JumpAndCultivate(cell, null);
    }

    private void HandleEpicCultivation(TerrainCell cell)
    {

        Action onEpicLeft = () => TryConvertLateralCell(cell, -1);
        Action onEpicRight = () => TryConvertLateralCell(cell, +1);

        if (cell.CurrentObject != null)
        {
            TerrainCell leftCell = GetLateralCell(cell, -1);
            TerrainCell rightCell = GetLateralCell(cell, +1);

            bool leftBlocked = leftCell == null || leftCell.CurrentObject != null;
            bool rightBlocked = rightCell == null || rightCell.CurrentObject != null;

            if (leftBlocked && rightBlocked)
            {
                return;
            }

            if (leftBlocked)
            {
                _owner.BeginAction();
                _cultivateAbility.JumpAndCultivate(rightCell, new CultivationParams
                {
                    OnCultivate = () =>
                    {
                        rightCell.TryConvertToFarm();
                        _owner.Experience.Add(_cultivateExperience);
                    }
                });
                return;
            }

            if (rightBlocked)
            {
                _owner.BeginAction();
                _cultivateAbility.JumpAndCultivate(leftCell, new CultivationParams
                {
                    OnCultivate = () =>
                    {
                        leftCell.TryConvertToFarm();
                        _owner.Experience.Add(_cultivateExperience);
                    }
                });
                return;
            }

            _owner.BeginAction();
            _cultivateAbility.JumpAndCultivate(leftCell, new CultivationParams
            {
                OnCultivate = () =>
                {
                    leftCell.TryConvertToFarm();
                    _owner.Experience.Add(_cultivateExperience);
                },
                EpicLook = true,
                EpicLookLeft = false,
                EpicLookRight = true,
                EpicRightLookDuration = _epicThreeTileLookDuration,
                OnEpicLookRightMid = () => TryConvertLateralCell(leftCell, +1),
                OnEpicLookRight = () => TryConvertLateralCell(leftCell, +2)
            });
            return;
        }

        if (cell.FarmTile == null || !cell.FarmTile.gameObject.activeSelf)
        {
            _owner.BeginAction();
            _cultivateAbility.JumpAndCultivate(cell, new CultivationParams
            {
                OnCultivate = () =>
                {
                    cell.TryConvertToFarm();
                    _owner.Experience.Add(_cultivateExperience);
                },
                EpicLook = true,
                OnEpicLookLeft = onEpicLeft,
                OnEpicLookRight = onEpicRight
            });
            return;
        }

        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null) return;

        _owner.BeginAction();

        _cultivateAbility.JumpAndCultivate(cell, new CultivationParams
        {
            EpicLook = true,
            OnEpicLookLeft = onEpicLeft,
            OnEpicLookRight = onEpicRight
        });
    }

    private void HandleLegendaryCultivation(TerrainCell cell)
    {
        if (cell.FarmTile == null || !cell.FarmTile.gameObject.activeSelf)
        {
            _owner.BeginAction();
            _cultivateAbility.JumpAndCultivate(cell, new CultivationParams
            {
                OnCultivate = () =>
                {
                    cell.TryConvertToFarm();
                    _owner.Experience.Add(_cultivateExperience);

                    foreach (TerrainCell lateralCell in GetLateralCells(cell))
                    {
                        if (lateralCell.FarmTile == null || !lateralCell.FarmTile.gameObject.activeSelf)
                        {
                            lateralCell.TryConvertToFarm();
                        }
                    }
                },
                Spin = true
            });
            return;
        }

        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null) return;

        List<TerrainCell> lateralCells = GetLateralCells(cell);
        bool anyLateralToConvert = lateralCells.Exists(c => c.FarmTile == null || !c.FarmTile.gameObject.activeSelf);

        _owner.BeginAction();
        _cultivateAbility.JumpAndCultivate(cell, new CultivationParams
        {
            OnCultivate = anyLateralToConvert ? () =>
            {
                bool anyConverted = false;
                foreach (TerrainCell lateralCell in lateralCells)
                {
                    if (lateralCell.FarmTile == null || !lateralCell.FarmTile.gameObject.activeSelf)
                    {
                        lateralCell.TryConvertToFarm();
                        anyConverted = true;
                    }
                }
                if (anyConverted) _owner.Experience.Add(_cultivateExperience);
            } : null,
            Spin = true
        });
    }

    public void InteractSecondary(TerrainCell cell)
    {
        if (_owner.IsMine && _isActing) return;

        SeedItemDataSO selectedSeed = _seedSelector?.SelectedSeed;
        if (selectedSeed == null) return;

        List<FarmTile> farmTiles = GetSowableFarmTiles(cell);
        if (farmTiles.Count == 0) return;

        if (_owner.Grade.CurrentGrade == EHelperGrade.Normal)
        {
            StartSow(farmTiles, selectedSeed);
        }
        else
        {
            StartCoroutine(FloatAndSow(cell, selectedSeed, farmTiles));
        }

        var pos = cell.GridPosition;
        _owner.PhotonView.RpcSafe(
            nameof(RPC_PlantSeed), RpcTarget.Others,
            pos.x, pos.y, pos.z, selectedSeed.Id);
    }

    private bool _anySeedPlanted = false;

    private IEnumerator FloatAndSow(TerrainCell cell, SeedItemDataSO seed, List<FarmTile> farmTiles)
    {
        if (_owner.PlayerOwner == null) yield break;

        _isActing = true;
        _anySeedPlanted = false;
        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.EatGround);

        HelperFollowAbility followAbility = _owner.GetAbility<HelperFollowAbility>();
        if (followAbility != null) followAbility.enabled = false;

        Vector3 originalPos = _owner.transform.position;

        Vector3 floatPos = _owner.PlayerOwner.transform.position + Vector3.up * _floatAbovePlayerHeight;
        yield return _owner.transform.DOMove(floatPos, _floatMoveDuration)
            .SetEase(Ease.OutQuad).WaitForCompletion();

        Tween bobTween = _owner.transform.DOMoveY(floatPos.y + _bobAmplitude, _bobDuration)
            .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

        yield return new WaitForSeconds(_floatDuration);
        bobTween.Kill();

        bool done = false;
        switch (_owner.Grade.CurrentGrade)
        {
            case EHelperGrade.Epic:
                SpawnEpicSecondaryVFX(cell, seed, () => done = true);
                break;
            case EHelperGrade.Legendary:
                SpawnLegendarySecondaryVFX(cell, seed, () => done = true);
                break;
            default:
                SpawnNormalSecondaryVFX(farmTiles, seed, () => done = true);
                break;
        }

        yield return new WaitUntil(() => done);

        // 원래 위치로 부드럽게 복귀한 뒤 FollowAbility 재활성화
        yield return _owner.transform.DOMove(originalPos, _returnDuration)
            .SetEase(Ease.InOutQuad).WaitForCompletion();

        if (followAbility != null) followAbility.enabled = true;
        if (_anySeedPlanted) _owner.Experience.Add(_sowExperience);
        _animAbility?.Play(EHelperAnim.Idle);
        _isActing = false;
        _anySeedPlanted = false;
        _owner.EndAction();
    }

    private void SpawnNormalSecondaryVFX(List<FarmTile> farmTiles, SeedItemDataSO seed, Action onComplete)
    {
        foreach (FarmTile tile in farmTiles)
        {
            if (_mouthPoint != null && _seedVfxPrefab != null)
            {
                Vector3 spawnPos = _mouthPoint.position;
                Vector3 targetPos = tile.CropSpawnPoint != null ? tile.CropSpawnPoint.position : tile.transform.position;
                GameObject vfxObj = Instantiate(_seedVfxPrefab, spawnPos, Quaternion.identity);
                SowVFX sowVfx = vfxObj.GetComponent<SowVFX>();
                sowVfx?.Launch(targetPos, (targetPos - spawnPos).normalized);
            }
            tile.PlantSeed(seed);
            _anySeedPlanted = true;
        }
        onComplete?.Invoke();
    }

    private void SpawnEpicSecondaryVFX(TerrainCell centerCell, SeedItemDataSO seed, Action onComplete)
    {
        // 플레이어 기준 왼쪽 → 가운데 → 오른쪽 순서
        Vector3Int rightOffset = GetGridRightOffset();
        var orderedCells = new List<TerrainCell>();

        var leftCell  = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition - rightOffset);
        var rightCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset);

        if (leftCell  != null) orderedCells.Add(leftCell);
        orderedCells.Add(centerCell);
        if (rightCell != null) orderedCells.Add(rightCell);

        if (_epicSecondaryVFX == null)
        {
            foreach (TerrainCell cell in orderedCells)
            {
                FarmTile tile = GetFarmTile(cell);
                if (tile != null && tile.IsReadyToSow) { tile.PlantSeed(seed); _anySeedPlanted = true; }
            }
            onComplete?.Invoke();
            return;
        }

        _epicSecondaryVFX.SpawnEffects(_mouthPoint, orderedCells, cell =>
        {
            FarmTile tile = GetFarmTile(cell);
            if (tile != null && tile.IsReadyToSow) { tile.PlantSeed(seed); _anySeedPlanted = true; }
        }, onComplete, () => _animAbility?.Replay(EHelperAnim.EatGround));
    }

    private void SpawnLegendarySecondaryVFX(TerrainCell centerCell, SeedItemDataSO seed, Action onComplete)
    {
        List<TerrainCell> orderedCells = GetOrderedTargetCells(centerCell);

        if (_legendarySecondaryVFX == null)
        {
            foreach (TerrainCell cell in orderedCells)
            {
                FarmTile tile = GetFarmTile(cell);
                if (tile != null && tile.IsReadyToSow) { tile.PlantSeed(seed); _anySeedPlanted = true; }
            }
            onComplete?.Invoke();
            return;
        }

        _legendarySecondaryVFX.SpawnEffects(_mouthPoint, orderedCells, cell =>
        {
            FarmTile tile = GetFarmTile(cell);
            if (tile != null && tile.IsReadyToSow) { tile.PlantSeed(seed); _anySeedPlanted = true; }
        }, onComplete);
    }

    private void StartSow(List<FarmTile> farmTiles, SeedItemDataSO seed)
    {
        _isActing = true;
        _anySeedPlanted = false;
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
        if (_anySeedPlanted) _owner.Experience.Add(_sowExperience);
        _animAbility?.Play(EHelperAnim.Idle);
        _currentFarmTiles = null;
        _currentSeed = null;
        _isActing = false;
        _anySeedPlanted = false;
        _owner.EndAction();
    }

    private IEnumerator PlantAfterDelay(FarmTile farmTile, SeedItemDataSO seed)
    {
        yield return new WaitForSeconds(_sowDelay);
        if (farmTile == null) yield break;

        farmTile.PlantSeed(seed);
        _anySeedPlanted = true;
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

        HelperFollowAbility followAbility = _owner?.GetAbility<HelperFollowAbility>();
        if (followAbility != null) followAbility.enabled = true;

        _owner?.transform.DOKill();
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

    // 왼쪽(-extension) → 오른쪽(+extension) 순서로 정렬된 cell 목록 반환 (Legendary sweep용)
    private List<TerrainCell> GetOrderedTargetCells(TerrainCell centerCell)
    {
        Vector3Int rightOffset = GetGridRightOffset();
        int extension = _owner.Grade.GetRange() - 1;
        var cells = new List<TerrainCell>();
        for (int i = -extension; i <= extension; i++)
        {
            var cell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset * i);
            if (cell != null) cells.Add(cell);
        }
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

    private TerrainCell GetLateralCell(TerrainCell centerCell, int directionSign)
    {
        Vector3Int rightOffset = GetGridRightOffset();
        return TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset * directionSign);
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
