using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Photon.Pun;
using UnityEngine;

public class SowActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private GameObject _seedVfxPrefab;
    [SerializeField] private float _sowDelay = 0.5f;
    [SerializeField] private float _epicThreeTileLookDuration = 1.6f;

    [SerializeField] private int _cultivateExperience = 10;
    [SerializeField] private int _sowExperience = 10;

    [Header("Secondary")]
    [SerializeField] private float _floatAbovePlayerHeight = 2f;
    [SerializeField] private float _floatMoveDuration = 0.5f;
    [SerializeField] private float _floatDuration = 2f;
    [SerializeField] private float _bobAmplitude = 0.3f;
    [SerializeField] private float _bobDuration = 0.4f;
    [SerializeField] private float _returnDuration = 0.5f;

    [SerializeField] [Range(0f, 1f)] private float _vfxTriggerNormalizedTime = 0.65f;

    private CultivateAbility _cultivateAbility;
    private SeedSelectAbility _seedSelector;
    private HelperAnimationAbility _animAbility;
    private SowEpicSecondaryVFXAbility _epicSecondaryVFX;
    private SowLegendarySecondaryVFXAbility _legendarySecondaryVFX;
    private SowSeedVfxSpawner _seedVfxSpawner;

    private bool _isSecondaryActing;
    private bool _anySeedPlanted;
    private List<FarmTile> _currentFarmTiles;
    private SeedItemDataSO _currentSeed;

    protected override void Awake()
    {
        base.Awake();
        _cultivateAbility = _owner.GetAbility<CultivateAbility>();
        _seedSelector = _owner.GetAbility<SeedSelectAbility>();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
        _epicSecondaryVFX = _owner.GetAbility<SowEpicSecondaryVFXAbility>();
        _legendarySecondaryVFX = _owner.GetAbility<SowLegendarySecondaryVFXAbility>();
        _seedVfxSpawner = new SowSeedVfxSpawner(() => _mouthPoint, () => _seedVfxPrefab);
    }

    public bool CanInteractPrimary(TerrainCell cell)
    {
        return CanCultivate(cell);
    }

    public bool CanInteractSecondary(TerrainCell cell)
    {
        return CanSow(cell);
    }

    public void InteractPrimary(TerrainCell cell)
    {
        if (cell == null || _cultivateAbility == null)
            return;

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

    public void InteractSecondary(TerrainCell cell)
    {
        if (cell == null)
            return;
        if (_owner.IsMine && _isSecondaryActing)
            return;

        SeedItemDataSO selectedSeed = _seedSelector?.SelectedSeed;
        if (selectedSeed == null)
            return;

        List<FarmTile> farmTiles = GetSowableFarmTiles(cell);
        if (farmTiles.Count == 0)
            return;

        StartSecondaryAction(cell, selectedSeed, _owner.Grade.CurrentGrade);

        Vector3Int pos = cell.GridPosition;
        _owner.PhotonView.RpcSafe(
            nameof(RPC_PlantSeed), RpcTarget.Others,
            pos.x, pos.y, pos.z, selectedSeed.Id, (int)_owner.Grade.CurrentGrade);
    }

    public void SowOpen()
    {
        if (_currentFarmTiles == null || _currentFarmTiles.Count == 0 || _currentSeed == null || !_seedVfxSpawner.HasMouthPoint)
            return;

        foreach (FarmTile tile in _currentFarmTiles)
        {
            FarmTile capturedTile = tile;
            _seedVfxSpawner.SpawnTo(capturedTile);
            StartCoroutine(PlantAfterDelay(capturedTile, _currentSeed));
        }
    }

    public void SowClose()
    {
        CompleteSecondaryAction();
    }

    [PunRPC]
    internal void RPC_PlantSeed(int gridX, int gridY, int gridZ, int seedId, int grade)
    {
        TerrainCell centerCell = TerrainGridManager.Instance?.GetCell(new Vector3Int(gridX, gridY, gridZ));
        if (centerCell == null)
            return;

        SeedItemDataSO seed = TerrainGridManager.Instance.SeedDatabase?.GetById(seedId);
        if (seed == null)
            return;

        StartSecondaryAction(centerCell, seed, (EHelperGrade)grade);
    }

    private void StartSecondaryAction(TerrainCell centerCell, SeedItemDataSO seed, EHelperGrade grade)
    {
        switch (grade)
        {
            case EHelperGrade.Epic:
                StartCoroutine(FloatAndSow(centerCell, seed, grade));
                break;
            case EHelperGrade.Legendary:
                StartCoroutine(FloatAndSow(centerCell, seed, grade));
                break;
            default:
                StartNormalSecondary(GetSowableFarmTiles(centerCell), seed);
                break;
        }
    }

    private void StartNormalSecondary(List<FarmTile> farmTiles, SeedItemDataSO seed)
    {
        if (farmTiles == null || farmTiles.Count == 0 || seed == null)
            return;

        _isSecondaryActing = true;
        _anySeedPlanted = false;
        _currentFarmTiles = farmTiles;
        _currentSeed = seed;

        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.Sow);
    }

    private IEnumerator FloatAndSow(TerrainCell centerCell, SeedItemDataSO seed, EHelperGrade grade)
    {
        if (_owner.PlayerOwner == null)
            yield break;

        _isSecondaryActing = true;
        _anySeedPlanted = false;
        _currentFarmTiles = null;
        _currentSeed = null;

        _owner.BeginAction();

        HelperFollowAbility followAbility = _owner.GetAbility<HelperFollowAbility>();
        if (followAbility != null)
            followAbility.enabled = false;

        Vector3 originalPos = _owner.transform.position;
        Vector3 floatPos = _owner.PlayerOwner.transform.position + Vector3.up * _floatAbovePlayerHeight;
        Tween floatMoveTween = _owner.transform.DOMove(floatPos, _floatMoveDuration)
            .SetEase(Ease.OutQuad);

        bool done = false;
        switch (grade)
        {
            case EHelperGrade.Epic:
                SpawnEpicSecondaryVFX(centerCell, seed, () => done = true);
                break;
            case EHelperGrade.Legendary:
                _animAbility?.Replay(EHelperAnim.LegendarySow);
                SpawnLegendarySecondaryVFX(centerCell, seed, () => done = true);
                break;
            default:
                done = true;
                break;
        }

        yield return floatMoveTween.WaitForCompletion();

        Tween bobTween = null;
        if (!done && _floatDuration > 0f)
        {
            bobTween = _owner.transform.DOMoveY(floatPos.y + _bobAmplitude, _bobDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        yield return new WaitUntil(() => done);

        bobTween?.Kill();

        yield return _owner.transform.DOMove(originalPos, _returnDuration)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();

        if (followAbility != null)
            followAbility.enabled = true;

        CompleteSecondaryAction();
    }

    private void SpawnEpicSecondaryVFX(TerrainCell centerCell, SeedItemDataSO seed, System.Action onComplete)
    {
        Vector3Int rightOffset = GetGridRightOffset();
        var orderedCells = new List<TerrainCell>();

        TerrainCell leftCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition - rightOffset);
        TerrainCell rightCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset);

        if (leftCell != null)
            orderedCells.Add(leftCell);
        orderedCells.Add(centerCell);
        if (rightCell != null)
            orderedCells.Add(rightCell);

        if (_epicSecondaryVFX == null)
        {
            PlantCells(orderedCells, seed);
            onComplete?.Invoke();
            return;
        }

        _epicSecondaryVFX.SpawnEffects(_seedVfxSpawner.MouthPoint, orderedCells, cell =>
        {
            FarmTile tile = GetFarmTile(cell);
            if (tile != null && tile.IsReadyToSow)
                PlantSeed(tile, seed);
        }, onComplete, ReplayEpicSowAndWait);
    }

    private void SpawnLegendarySecondaryVFX(TerrainCell centerCell, SeedItemDataSO seed, System.Action onComplete)
    {
        List<TerrainCell> orderedCells = GetOrderedTargetCells(centerCell);

        if (_legendarySecondaryVFX == null)
        {
            PlantCells(orderedCells, seed);
            onComplete?.Invoke();
            return;
        }

        _legendarySecondaryVFX.SpawnEffects(_seedVfxSpawner.MouthPoint, orderedCells, cell =>
        {
            FarmTile tile = GetFarmTile(cell);
            if (tile != null && tile.IsReadyToSow)
                PlantSeed(tile, seed);
        }, onComplete);
    }

    private IEnumerator ReplayEpicSowAndWait()
    {
        if (_animAbility == null)
            yield break;

        yield return StartCoroutine(_animAbility.ForceReplayAndWait(EHelperAnim.EpicSow, _vfxTriggerNormalizedTime));
    }

    private void HandleNormalCultivation(TerrainCell cell)
    {
        if (NeedsFarmConversion(cell))
        {
            _owner.BeginAction();
            _cultivateAbility.JumpAndCultivate(cell, new CultivateAbility.CultivationParams
            {
                OnCultivate = () =>
                {
                    cell.TryConvertToFarm();
                    _owner.Experience.Add(_cultivateExperience);
                    ConvertLateralFarmTiles(cell);
                }
            });
            return;
        }

        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null)
            return;

        _owner.BeginAction();

        if (!farmTile.HasSeed)
        {
            _cultivateAbility.JumpAndCultivate(cell, new CultivateAbility.CultivationParams
            {
                OnCultivate = () => ConvertLateralFarmTiles(cell)
            });
            return;
        }

        _cultivateAbility.JumpAndCultivate(cell, null);
    }

    private void HandleEpicCultivation(TerrainCell cell)
    {
        System.Action onEpicLeft = () =>
        {
            TryConvertLateralCell(cell, -1);
            ReplayEpicSow();
        };
        System.Action onEpicRight = () =>
        {
            TryConvertLateralCell(cell, +1);
        };

        if (cell.CurrentObject != null)
        {
            TerrainCell leftCell = GetLateralCell(cell, -1);
            TerrainCell rightCell = GetLateralCell(cell, +1);

            bool leftBlocked = leftCell == null || leftCell.CurrentObject != null;
            bool rightBlocked = rightCell == null || rightCell.CurrentObject != null;

            if (leftBlocked && rightBlocked)
                return;

            if (leftBlocked)
            {
                CultivateSingleCellWithEpicReplay(rightCell);
                return;
            }

            if (rightBlocked)
            {
                CultivateSingleCellWithEpicReplay(leftCell);
                return;
            }

            _owner.BeginAction();
            _cultivateAbility.JumpAndCultivate(leftCell, new CultivateAbility.CultivationParams
            {
                OnCultivate = () =>
                {
                    leftCell.TryConvertToFarm();
                    _owner.Experience.Add(_cultivateExperience);
                    ReplayEpicSow();
                },
                EpicLook = true,
                EpicLookLeft = false,
                EpicLookRight = true,
                EpicRightLookDuration = _epicThreeTileLookDuration,
                OnEpicLookRightMid = () =>
                {
                    TryConvertLateralCell(leftCell, +1);
                    ReplayEpicSow();
                },
                OnEpicLookRight = () => TryConvertLateralCell(leftCell, +2),
                OnEpicLookRightRoutine = ReplayEpicSowAndWait
            });
            return;
        }

        if (NeedsFarmConversion(cell))
        {
            _owner.BeginAction();
            _cultivateAbility.JumpAndCultivate(cell, new CultivateAbility.CultivationParams
            {
                OnCultivate = () =>
                {
                    cell.TryConvertToFarm();
                    _owner.Experience.Add(_cultivateExperience);
                    ReplayEpicSow();
                },
                EpicLook = true,
                OnEpicLookLeft = onEpicLeft,
                OnEpicLookRight = onEpicRight,
                OnEpicLookRightRoutine = ReplayEpicSowAndWait
            });
            return;
        }

        if (GetFarmTile(cell) == null)
            return;

        _owner.BeginAction();
        _cultivateAbility.JumpAndCultivate(cell, new CultivateAbility.CultivationParams
        {
            EpicLook = true,
            OnEpicLookLeft = onEpicLeft,
            OnEpicLookRight = onEpicRight,
            OnEpicLookRightRoutine = ReplayEpicSowAndWait
        });
    }

    private void HandleLegendaryCultivation(TerrainCell cell)
    {
        if (cell.CurrentObject != null && cell.Data.ObjectType == EGridObjectType.Tree)
            return;

        if (NeedsFarmConversion(cell))
        {
            _owner.BeginAction();
            _cultivateAbility.JumpAndCultivate(cell, new CultivateAbility.CultivationParams
            {
                OnCultivate = () =>
                {
                    cell.TryConvertToFarm();
                    _owner.Experience.Add(_cultivateExperience);
                    ConvertLateralFarmTiles(cell);
                },
                Spin = true
            });
            return;
        }

        FarmTile farmTile = GetFarmTile(cell);
        if (farmTile == null)
            return;

        List<TerrainCell> lateralCells = GetLateralCells(cell);
        bool anyLateralToConvert = lateralCells.Exists(NeedsFarmConversion);

        _owner.BeginAction();
        _cultivateAbility.JumpAndCultivate(cell, new CultivateAbility.CultivationParams
        {
            OnCultivate = anyLateralToConvert ? () =>
            {
                bool anyConverted = ConvertFarmTiles(lateralCells);
                if (anyConverted)
                    _owner.Experience.Add(_cultivateExperience);
            } : null,
            Spin = true
        });
    }

    private void CultivateSingleCellWithEpicReplay(TerrainCell cell)
    {
        _owner.BeginAction();
        _cultivateAbility.JumpAndCultivate(cell, new CultivateAbility.CultivationParams
        {
            OnCultivate = () =>
            {
                cell.TryConvertToFarm();
                _owner.Experience.Add(_cultivateExperience);
                ReplayEpicSow();
            }
        });
    }

    private void ReplayEpicSow()
    {
        StartCoroutine(ReplayEpicSowAndWait());
    }

    private void ConvertLateralFarmTiles(TerrainCell centerCell)
    {
        ConvertFarmTiles(GetLateralCells(centerCell));
    }

    private bool ConvertFarmTiles(List<TerrainCell> cells)
    {
        bool anyConverted = false;
        foreach (TerrainCell lateralCell in cells)
        {
            if (NeedsFarmConversion(lateralCell))
            {
                lateralCell.TryConvertToFarm();
                anyConverted = true;
            }
        }

        return anyConverted;
    }

    private bool CanCultivate(TerrainCell cell)
    {
        if (cell == null)
            return false;
        if (cell.Data.CellType != ECellType.Dirt)
            return false;
        if (cell.Data.ObjectType != EGridObjectType.None && cell.Data.ObjectType != EGridObjectType.FarmLand)
            return false;
        return true;
    }

    private bool CanSow(TerrainCell cell)
    {
        FarmTile farmTile = GetFarmTile(cell);
        return farmTile != null && farmTile.IsReadyToSow;
    }

    private FarmTile GetFarmTile(TerrainCell cell)
    {
        if (cell == null)
            return null;
        if (cell.FarmTile != null && cell.FarmTile.gameObject.activeSelf)
            return cell.FarmTile;
        return null;
    }

    private bool NeedsFarmConversion(TerrainCell cell)
    {
        return cell == null || cell.FarmTile == null || !cell.FarmTile.gameObject.activeSelf;
    }

    private List<TerrainCell> GetLateralCells(TerrainCell centerCell)
    {
        var cells = new List<TerrainCell>();
        int extension = _owner.Grade.GetRange() - 1;
        if (extension <= 0)
            return cells;

        Vector3Int rightOffset = GetGridRightOffset();
        for (int i = 1; i <= extension; i++)
        {
            TerrainCell rightCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset * i);
            TerrainCell leftCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition - rightOffset * i);
            if (rightCell != null)
                cells.Add(rightCell);
            if (leftCell != null)
                cells.Add(leftCell);
        }

        return cells;
    }

    private List<TerrainCell> GetTargetCells(TerrainCell centerCell)
    {
        var cells = new List<TerrainCell> { centerCell };
        cells.AddRange(GetLateralCells(centerCell));
        return cells;
    }

    private List<TerrainCell> GetOrderedTargetCells(TerrainCell centerCell)
    {
        Vector3Int rightOffset = GetGridRightOffset();
        int extension = _owner.Grade.GetRange() - 1;
        var cells = new List<TerrainCell>();
        for (int i = -extension; i <= extension; i++)
        {
            TerrainCell cell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset * i);
            if (cell != null)
                cells.Add(cell);
        }

        return cells;
    }

    private List<FarmTile> GetSowableFarmTiles(TerrainCell centerCell)
    {
        var tiles = new List<FarmTile>();
        foreach (TerrainCell cell in GetTargetCells(centerCell))
        {
            FarmTile tile = GetFarmTile(cell);
            if (tile != null && tile.IsReadyToSow)
                tiles.Add(tile);
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
        TerrainCell lateralCell = GetLateralCell(centerCell, directionSign);
        if (lateralCell != null && NeedsFarmConversion(lateralCell))
            lateralCell.TryConvertToFarm();
    }

    private Vector3Int GetGridRightOffset()
    {
        if (_owner.PlayerOwner == null)
            return Vector3Int.right;

        Vector3 right = _owner.PlayerOwner.transform.right;
        return new Vector3Int(Mathf.RoundToInt(right.x), 0, Mathf.RoundToInt(right.z));
    }

    private void PlantCells(List<TerrainCell> cells, SeedItemDataSO seed)
    {
        foreach (TerrainCell cell in cells)
        {
            FarmTile tile = GetFarmTile(cell);
            if (tile != null && tile.IsReadyToSow)
                PlantSeed(tile, seed);
        }
    }

    private IEnumerator PlantAfterDelay(FarmTile farmTile, SeedItemDataSO seed)
    {
        yield return new WaitForSeconds(_sowDelay);
        if (farmTile == null)
            yield break;

        PlantSeed(farmTile, seed);
    }

    private void PlantSeed(FarmTile farmTile, SeedItemDataSO seed)
    {
        farmTile.PlantSeed(seed);
        _anySeedPlanted = true;
    }

    private void CompleteSecondaryAction()
    {
        if (_anySeedPlanted)
            _owner.Experience.Add(_sowExperience);

        _animAbility?.Play(EHelperAnim.Idle);
        _currentFarmTiles = null;
        _currentSeed = null;
        _isSecondaryActing = false;
        _anySeedPlanted = false;
        _owner.EndAction();
    }

    private void CancelCurrentAction()
    {
        _currentFarmTiles = null;
        _currentSeed = null;
        _isSecondaryActing = false;
        _anySeedPlanted = false;

        HelperFollowAbility followAbility = _owner?.GetAbility<HelperFollowAbility>();
        if (followAbility != null)
            followAbility.enabled = true;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        CancelCurrentAction();
        _owner?.transform.DOKill();
        _owner?.EndAction();
    }
}
