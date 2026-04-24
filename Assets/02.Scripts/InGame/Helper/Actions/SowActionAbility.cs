using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Photon.Pun;
using UnityEngine;

public class SowActionAbility : HelperAbility, IHelperAction, ISecondaryInteractBlockNotifier
{
    private sealed class SowPlantPlan
    {
        public TerrainCell Cell;
        public FarmTile Tile;
        public SeedItemDataSO Seed;
    }

    private const string NoSeedItemMessage = "씨앗 아이템이 없습니다";

    private const string UnavailableSeedGradeMessage = "아직 심을 수 없는 등급의 씨앗입니다";

    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private GameObject _seedVfxPrefab;
    [SerializeField] private GameObject _cultivateGroundEffectPrefab;
    [SerializeField] private Transform _cultivateLegendarySpinEffect;
    [SerializeField] private float _sowDelay = 0.5f;
    [SerializeField] private float _epicThreeTileLookDuration = 1.6f;
    [SerializeField] private float _cultivateGroundEffectLifetime = 2f;
    [SerializeField] private float _cultivateGroundEffectSurfaceOffset = 2.1f;
    [SerializeField] private float _cultivateLegendarySpinFadeOutDuration = 0.05f;
    [SerializeField] private float _secondaryOpenFallbackDelay = 0.35f;
    [SerializeField] private float _secondaryCompleteFallbackDelay = 5f;
    [SerializeField, Min(0f)] private float _epicCultivateSfxMinInterval = 0.15f;

    [SerializeField] private int _cultivateExperience = 10;
    [SerializeField] private int _sowExperience = 10;

    private CultivateAbility _cultivateAbility;
    private SeedSelectAbility _seedSelector;
    private HelperAnimationAbility _animAbility;
    private SowSecondaryVFXAbility _secondaryPresentation;
    private SowSeedVfxSpawner _seedVfxSpawner;
    private Tween _cultivateLegendarySpinHideTween;
    private ParticleSystem[] _cultivateLegendarySpinParticles;

    private bool _isSecondaryActing;
    private bool _anySeedPlanted;
    private List<SowPlantPlan> _currentPlantPlan;
    private Coroutine _secondaryOpenFallbackCoroutine;
    private Coroutine _secondaryCompleteFallbackCoroutine;
    private bool _secondaryOpened;
    private bool _showNoSeedMessageOnSecondaryComplete;
    private bool _showUnavailableSeedGradeMessageOnSecondaryComplete;
    private float _lastEpicCultivateSfxTime = float.MinValue;

    protected override void Awake()
    {
        base.Awake();
        _cultivateAbility = _owner.GetAbility<CultivateAbility>();
        _seedSelector = _owner.GetAbility<SeedSelectAbility>();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
        _secondaryPresentation = _owner.GetAbility<SowSecondaryVFXAbility>();
        _seedVfxSpawner = new SowSeedVfxSpawner(() => _mouthPoint, () => _seedVfxPrefab);
        _cultivateLegendarySpinParticles = _cultivateLegendarySpinEffect != null
            ? _cultivateLegendarySpinEffect.GetComponentsInChildren<ParticleSystem>(true)
            : System.Array.Empty<ParticleSystem>();
        SetCultivateLegendarySpinEffectActive(false, true);
    }

    public bool CanInteractPrimary(TerrainCell cell)
    {
        return CanCultivate(cell);
    }

    public bool CanInteractSecondary(TerrainCell cell)
    {
        if (IsSowBlockedByDungeon())
            return false;

        return HasSowableTarget(cell, _owner != null ? _owner.Grade.CurrentGrade : EHelperGrade.Normal)
            && EnsureSeedReadyForInteraction(showNoSeedMessage: false);
    }

    public void InteractPrimary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null || _cultivateAbility == null)
            return;

        if (_owner.IsMine)
            _seedSelector?.ClearSelection();

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
        cell = GetInteractableCell(cell);
        if (cell == null)
            return;
        if (_owner.IsMine && _isSecondaryActing)
            return;
        if (IsSowBlockedByDungeon())
            return;
        if (!HasSowableTarget(cell, _owner.Grade.CurrentGrade))
            return;

        if (!EnsureSeedReadyForInteraction(showNoSeedMessage: true))
            return;

        EHelperGrade grade = _owner.Grade.CurrentGrade;
        List<SowPlantPlan> targets = GetSowPlantTargets(cell, grade);
        if (targets.Count == 0)
            return;

        List<SowPlantPlan> plantPlan = BuildLocalSowPlantPlan(
            targets,
            out bool hasUnplannedTargets,
            out bool hasUnavailableSeedGradeTargets);
        if (plantPlan.Count == 0)
        {
            if (hasUnavailableSeedGradeTargets)
                ShowUnavailableSeedGradeMessage();
            else
                ShowNoSeedItemMessage();
            return;
        }

        StartSecondaryAction(plantPlan, grade, hasUnplannedTargets, hasUnavailableSeedGradeTargets);
        SendPlantPlanRpc(plantPlan, grade);
    }

    public void SowOpen()
    {
        if (!_isSecondaryActing || _secondaryOpened)
            return;

        _secondaryOpened = true;
        StopSecondaryOpenFallback();

        if (_currentPlantPlan == null || _currentPlantPlan.Count == 0 || !_seedVfxSpawner.HasMouthPoint)
            return;

        foreach (SowPlantPlan plan in _currentPlantPlan)
        {
            FarmTile capturedTile = plan.Tile;
            SeedItemDataSO capturedSeed = plan.Seed;
            _seedVfxSpawner.SpawnTo(capturedTile, () => PlaySowNormalImpactSfx(capturedTile));
            StartCoroutine(PlantAfterDelay(capturedTile, capturedSeed));
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

        List<SowPlantPlan> targets = GetSowPlantTargets(centerCell, (EHelperGrade)grade);
        foreach (SowPlantPlan target in targets)
            target.Seed = seed;

        StartSecondaryAction(targets, (EHelperGrade)grade);
    }

    [PunRPC]
    internal void RPC_PlantSeedPlan(int grade, int[] gridXs, int[] gridYs, int[] gridZs, int[] seedIds)
    {
        List<SowPlantPlan> plantPlan = BuildRemoteSowPlantPlan(gridXs, gridYs, gridZs, seedIds);
        if (plantPlan.Count == 0)
            return;

        StartSecondaryAction(plantPlan, (EHelperGrade)grade);
    }

    private void StartSecondaryAction(
        List<SowPlantPlan> plantPlan,
        EHelperGrade grade,
        bool showNoSeedMessageOnComplete = false,
        bool showUnavailableSeedGradeMessageOnComplete = false)
    {
        if (plantPlan == null || plantPlan.Count == 0)
            return;

        switch (grade)
        {
            case EHelperGrade.Epic:
            case EHelperGrade.Legendary:
                StartGradeSecondary(
                    plantPlan,
                    grade,
                    showNoSeedMessageOnComplete,
                    showUnavailableSeedGradeMessageOnComplete);
                break;
            default:
                StartNormalSecondary(
                    plantPlan,
                    showNoSeedMessageOnComplete,
                    showUnavailableSeedGradeMessageOnComplete);
                break;
        }
    }

    private void StartNormalSecondary(
        List<SowPlantPlan> plantPlan,
        bool showNoSeedMessageOnComplete = false,
        bool showUnavailableSeedGradeMessageOnComplete = false)
    {
        if (plantPlan == null || plantPlan.Count == 0)
            return;

        _isSecondaryActing = true;
        _anySeedPlanted = false;
        _currentPlantPlan = plantPlan;
        _secondaryOpened = false;
        _showNoSeedMessageOnSecondaryComplete = showNoSeedMessageOnComplete;
        _showUnavailableSeedGradeMessageOnSecondaryComplete = showUnavailableSeedGradeMessageOnComplete;

        _owner.BeginAction();
        StartSecondaryFallbacks(requireOpenFallback: true);
        _animAbility?.Play(EHelperAnim.Sow);
    }

    private void StartGradeSecondary(
        List<SowPlantPlan> plantPlan,
        EHelperGrade grade,
        bool showNoSeedMessageOnComplete = false,
        bool showUnavailableSeedGradeMessageOnComplete = false)
    {
        if (plantPlan == null || plantPlan.Count == 0)
            return;

        _isSecondaryActing = true;
        _anySeedPlanted = false;
        _currentPlantPlan = null;
        _secondaryOpened = false;
        _showNoSeedMessageOnSecondaryComplete = showNoSeedMessageOnComplete;
        _showUnavailableSeedGradeMessageOnSecondaryComplete = showUnavailableSeedGradeMessageOnComplete;

        _owner.BeginAction();
        StartSecondaryFallbacks(requireOpenFallback: false);

        if (_secondaryPresentation == null)
        {
            PlantPlan(plantPlan);
            CompleteSecondaryAction();
            return;
        }

        List<TerrainCell> orderedCells = GetCellsFromPlantPlan(plantPlan);

        switch (grade)
        {
            case EHelperGrade.Epic:
                _secondaryPresentation.PlayEpic(
                    _seedVfxSpawner.MouthPoint,
                    orderedCells,
                    cell =>
                    {
                        SowPlantPlan plan = FindPlantPlanForCell(plantPlan, cell);
                        if (plan != null && plan.Tile != null && plan.Tile.IsReadyToSow)
                        {
                            PlaySowNormalImpactSfx(plan.Tile);
                            PlantSeed(plan.Tile, plan.Seed);
                        }
                    },
                    CompleteSecondaryAction);
                break;
            case EHelperGrade.Legendary:
                bool playedLegendaryImpactSfx = false;
                _secondaryPresentation.PlayLegendary(
                    _seedVfxSpawner.MouthPoint,
                    orderedCells,
                    cell =>
                    {
                        SowPlantPlan plan = FindPlantPlanForCell(plantPlan, cell);
                        if (plan != null && plan.Tile != null && plan.Tile.IsReadyToSow)
                        {
                            if (!playedLegendaryImpactSfx)
                            {
                                PlaySowLegendaryImpactSfx(plan.Tile);
                                playedLegendaryImpactSfx = true;
                            }
                            PlantSeed(plan.Tile, plan.Seed);
                        }
                    },
                    CompleteSecondaryAction);
                break;
        }
    }

    private List<TerrainCell> GetEpicOrderedTargetCells(TerrainCell centerCell)
    {
        centerCell = GetInteractableCell(centerCell);
        if (centerCell == null)
            return new List<TerrainCell>();

        Vector3Int rightOffset = GetGridRightOffset();
        var orderedCells = new List<TerrainCell>();

        TerrainCell leftCell = GetGridInteractableCell(centerCell.GridPosition - rightOffset);
        TerrainCell rightCell = GetGridInteractableCell(centerCell.GridPosition + rightOffset);

        if (leftCell != null)
            orderedCells.Add(leftCell);
        orderedCells.Add(centerCell);
        if (rightCell != null)
            orderedCells.Add(rightCell);

        return orderedCells;
    }

    private IEnumerator ReplayEpicSowAndWait()
    {
        yield return ReplayEpicSowAndWait(true);
    }

    private IEnumerator ReplayEpicSowAndWait(bool playSfx)
    {
        if (_secondaryPresentation == null)
            yield break;

        if (playSfx)
            PlayEpicCultivateSfx();
        yield return StartCoroutine(_secondaryPresentation.ReplayEpicSowAndWait());
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
                    PlayNormalCultivateSfx();
                    if (cell.TryConvertToFarm())
                    {
                        _owner.Experience.Add(_cultivateExperience);
                        BroadcastTerrainCellStateFromMaster(cell);
                    }
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
                OnCultivate = () =>
                {
                    PlayNormalCultivateSfx();
                    ConvertLateralFarmTiles(cell);
                }
            });
            return;
        }

        _cultivateAbility.JumpAndCultivate(cell, new CultivateAbility.CultivationParams
        {
            OnCultivate = PlayNormalCultivateSfx
        });
    }

    private void HandleEpicCultivation(TerrainCell cell)
    {
        bool grantedCultivateExperience = false;
        void GrantCultivateExperienceOnce(bool converted)
        {
            if (!converted || grantedCultivateExperience)
                return;

            grantedCultivateExperience = true;
            _owner.Experience.Add(_cultivateExperience);
        }

        System.Action onEpicLeft = () =>
        {
            GrantCultivateExperienceOnce(TryConvertLateralCell(cell, -1));
            ReplayEpicSow();
        };
        System.Action onEpicRight = () =>
        {
            GrantCultivateExperienceOnce(TryConvertLateralCell(cell, +1));
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
                    PlayEpicCultivateSfx();
                    bool convertedLeft = TryConvertToFarmWithCultivateEffect(leftCell, true);
                    GrantCultivateExperienceOnce(convertedLeft);
                    if (convertedLeft)
                        BroadcastTerrainCellStateFromMaster(leftCell);
                    ReplayEpicSow(false);
                },
                EpicLook = true,
                EpicLookLeft = false,
                EpicLookRight = true,
                EpicRightLookDuration = _epicThreeTileLookDuration,
                OnEpicLookRightMid = () =>
                {
                    GrantCultivateExperienceOnce(TryConvertLateralCell(leftCell, +1));
                    ReplayEpicSow();
                },
                OnEpicLookRight = () => GrantCultivateExperienceOnce(TryConvertLateralCell(leftCell, +2)),
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
                    PlayEpicCultivateSfx();
                    bool convertedCenter = TryConvertToFarmWithCultivateEffect(cell, true);
                    GrantCultivateExperienceOnce(convertedCenter);
                    if (convertedCenter)
                        BroadcastTerrainCellStateFromMaster(cell);
                    ReplayEpicSow(false);
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
            OnCultivate = PlayEpicCultivateSfx,
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
                bool convertedCenter = TryConvertToFarmWithCultivateEffect(cell, true);
                bool convertedLateral = ConvertLateralFarmTiles(cell, true);
                if (convertedCenter || convertedLateral)
                {
                    _owner.Experience.Add(_cultivateExperience);
                    BroadcastTerrainCellStateFromMaster(cell);
                }
            },
            OnSpinStart = PlayCultivateLegendarySpinEffect,
            OnSpinComplete = StopCultivateLegendarySpinEffect,
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
                bool anyConverted = ConvertFarmTiles(lateralCells, true);
                if (anyConverted)
                    _owner.Experience.Add(_cultivateExperience);
            } : null,
            OnSpinStart = PlayCultivateLegendarySpinEffect,
            OnSpinComplete = StopCultivateLegendarySpinEffect,
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
                PlayEpicCultivateSfx();
                if (TryConvertToFarmWithCultivateEffect(cell, true))
                {
                    _owner.Experience.Add(_cultivateExperience);
                    BroadcastTerrainCellStateFromMaster(cell);
                }
                ReplayEpicSow(false);
            }
        });
    }

    private void ReplayEpicSow(bool playSfx = true)
    {
        if (playSfx)
            PlayEpicCultivateSfx();
        _secondaryPresentation?.ReplayEpicSow();
    }

    private bool ConvertLateralFarmTiles(TerrainCell centerCell, bool spawnCultivateEffect = false)
    {
        return ConvertFarmTiles(GetLateralCells(centerCell), spawnCultivateEffect);
    }

    private bool ConvertFarmTiles(List<TerrainCell> cells, bool spawnCultivateEffect = false)
    {
        bool anyConverted = false;
        foreach (TerrainCell lateralCell in cells)
        {
            if (NeedsFarmConversion(lateralCell))
            {
                anyConverted |= TryConvertToFarmWithCultivateEffect(lateralCell, spawnCultivateEffect);
                BroadcastTerrainCellStateFromMaster(lateralCell);
            }
        }

        return anyConverted;
    }

    private bool CanCultivate(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
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
        return IsTileReadyForSeedPlanting(farmTile);
    }

    private FarmTile GetFarmTile(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null)
            return null;
        if (cell.FarmTile != null && cell.FarmTile.gameObject.activeSelf)
            return cell.FarmTile;
        return null;
    }

    private bool NeedsFarmConversion(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        return cell != null && (cell.FarmTile == null || !cell.FarmTile.gameObject.activeSelf);
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
            rightCell = GetInteractableCell(rightCell);
            leftCell = GetInteractableCell(leftCell);
            if (rightCell != null)
                cells.Add(rightCell);
            if (leftCell != null)
                cells.Add(leftCell);
        }

        return cells;
    }

    private List<TerrainCell> GetTargetCells(TerrainCell centerCell)
    {
        centerCell = GetInteractableCell(centerCell);
        if (centerCell == null)
            return new List<TerrainCell>();

        var cells = new List<TerrainCell> { centerCell };
        cells.AddRange(GetLateralCells(centerCell));
        return cells;
    }

    private List<TerrainCell> GetOrderedTargetCells(TerrainCell centerCell)
    {
        centerCell = GetInteractableCell(centerCell);
        if (centerCell == null)
            return new List<TerrainCell>();

        Vector3Int rightOffset = GetGridRightOffset();
        int extension = _owner.Grade.GetRange() - 1;
        var cells = new List<TerrainCell>();
        for (int i = -extension; i <= extension; i++)
        {
            TerrainCell cell = GetGridInteractableCell(centerCell.GridPosition + rightOffset * i);
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

    private List<SowPlantPlan> GetSowPlantTargets(TerrainCell centerCell, EHelperGrade grade)
    {
        if (IsSowBlockedByDungeon())
            return new List<SowPlantPlan>();

        List<TerrainCell> cells = grade switch
        {
            EHelperGrade.Epic => GetEpicOrderedTargetCells(centerCell),
            EHelperGrade.Legendary => GetOrderedTargetCells(centerCell),
            _ => GetTargetCells(centerCell)
        };

        var targets = new List<SowPlantPlan>();
        foreach (TerrainCell cell in cells)
        {
            FarmTile tile = GetFarmTile(cell);
            if (IsTileReadyForSeedPlanting(tile))
            {
                targets.Add(new SowPlantPlan
                {
                    Cell = cell,
                    Tile = tile
                });
            }
        }

        return targets;
    }

    private List<SowPlantPlan> BuildLocalSowPlantPlan(
        List<SowPlantPlan> targets,
        out bool hasUnplannedTargets,
        out bool hasUnavailableSeedGradeTargets)
    {
        hasUnplannedTargets = false;
        hasUnavailableSeedGradeTargets = false;
        var plantPlan = new List<SowPlantPlan>();
        if (targets == null || targets.Count == 0)
            return plantPlan;

        PlayerInventoryAbility inventory = _owner.PlayerOwner?.GetAbility<PlayerInventoryAbility>();
        if (inventory == null)
        {
            hasUnplannedTargets = true;
            return plantPlan;
        }

        Dictionary<SeedItemDataSO, int> availableCounts = BuildSeedCountSnapshot(inventory);
        SeedItemDataSO seed = HasAvailableSelectedSeed()
            ? _seedSelector.SelectedSeed
            : FindFirstAvailableSeedInSnapshot(inventory, availableCounts);

        if (seed != null && !CanCurrentHelperPlantSeed(seed))
        {
            hasUnavailableSeedGradeTargets = true;
            return plantPlan;
        }

        if (seed != null)
            _seedSelector?.TrySelectSeed(seed);

        foreach (SowPlantPlan target in targets)
        {
            if (!HasSeedCountInSnapshot(seed, availableCounts))
            {
                seed = FindFirstAvailableSeedInSnapshot(inventory, availableCounts);
                if (seed != null && !CanCurrentHelperPlantSeed(seed))
                {
                    hasUnavailableSeedGradeTargets = true;
                    break;
                }

                if (seed != null)
                    _seedSelector?.TrySelectSeed(seed);
            }

            if (!HasSeedCountInSnapshot(seed, availableCounts))
            {
                hasUnplannedTargets = true;
                break;
            }

            availableCounts[seed]--;
            plantPlan.Add(new SowPlantPlan
            {
                Cell = target.Cell,
                Tile = target.Tile,
                Seed = seed
            });
        }

        hasUnplannedTargets |= plantPlan.Count < targets.Count;
        return plantPlan;
    }

    private List<SowPlantPlan> BuildRemoteSowPlantPlan(int[] gridXs, int[] gridYs, int[] gridZs, int[] seedIds)
    {
        var plantPlan = new List<SowPlantPlan>();
        if (gridXs == null || gridYs == null || gridZs == null || seedIds == null)
            return plantPlan;

        int count = Mathf.Min(gridXs.Length, gridYs.Length, gridZs.Length, seedIds.Length);
        for (int i = 0; i < count; i++)
        {
            TerrainCell cell = TerrainGridManager.Instance?.GetCell(new Vector3Int(gridXs[i], gridYs[i], gridZs[i]));
            if (cell == null)
                continue;

            SeedItemDataSO seed = TerrainGridManager.Instance.SeedDatabase?.GetById(seedIds[i]);
            FarmTile tile = GetFarmTile(cell);
            if (seed == null || !IsTileReadyForSeedPlanting(tile))
                continue;

            plantPlan.Add(new SowPlantPlan
            {
                Cell = cell,
                Tile = tile,
                Seed = seed
            });
        }

        return plantPlan;
    }

    private Dictionary<SeedItemDataSO, int> BuildSeedCountSnapshot(PlayerInventoryAbility inventory)
    {
        var counts = new Dictionary<SeedItemDataSO, int>();
        if (inventory == null)
            return counts;

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            InventorySlot slot = inventory.GetSlot(i);
            if (slot == null || slot.IsEmpty || !SeedSelectAbility.IsSeedItem(slot.Item))
                continue;

            SeedItemDataSO seed = (SeedItemDataSO)slot.Item;
            counts.TryGetValue(seed, out int count);
            counts[seed] = count + slot.Count;
        }

        return counts;
    }

    private static bool HasSeedCountInSnapshot(SeedItemDataSO seed, Dictionary<SeedItemDataSO, int> counts)
    {
        return seed != null && counts != null && counts.TryGetValue(seed, out int count) && count > 0;
    }

    private SeedItemDataSO FindFirstAvailableSeedInSnapshot(PlayerInventoryAbility inventory, Dictionary<SeedItemDataSO, int> counts)
    {
        if (inventory == null || counts == null)
            return null;

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            InventorySlot slot = inventory.GetSlot(i);
            if (slot == null || slot.IsEmpty || !SeedSelectAbility.IsSeedItem(slot.Item))
                continue;

            SeedItemDataSO seed = (SeedItemDataSO)slot.Item;
            if (HasSeedCountInSnapshot(seed, counts))
                return seed;
        }

        return null;
    }

    private static List<TerrainCell> GetCellsFromPlantPlan(List<SowPlantPlan> plantPlan)
    {
        var cells = new List<TerrainCell>();
        if (plantPlan == null)
            return cells;

        foreach (SowPlantPlan plan in plantPlan)
        {
            if (plan?.Cell != null)
                cells.Add(plan.Cell);
        }

        return cells;
    }

    private static SowPlantPlan FindPlantPlanForCell(List<SowPlantPlan> plantPlan, TerrainCell cell)
    {
        if (plantPlan == null || cell == null)
            return null;

        foreach (SowPlantPlan plan in plantPlan)
        {
            if (plan != null && plan.Cell == cell)
                return plan;
        }

        return null;
    }

    private void SendPlantPlanRpc(List<SowPlantPlan> plantPlan, EHelperGrade grade)
    {
        if (plantPlan == null || plantPlan.Count == 0)
            return;

        int count = plantPlan.Count;
        int[] gridXs = new int[count];
        int[] gridYs = new int[count];
        int[] gridZs = new int[count];
        int[] seedIds = new int[count];

        for (int i = 0; i < count; i++)
        {
            Vector3Int gridPosition = plantPlan[i].Cell.GridPosition;
            gridXs[i] = gridPosition.x;
            gridYs[i] = gridPosition.y;
            gridZs[i] = gridPosition.z;
            seedIds[i] = plantPlan[i].Seed.Id;
        }

        _owner.PhotonView.RpcSafe(
            nameof(RPC_PlantSeedPlan), RpcTarget.Others,
            (int)grade, gridXs, gridYs, gridZs, seedIds);
    }

    private TerrainCell GetLateralCell(TerrainCell centerCell, int directionSign)
    {
        Vector3Int rightOffset = GetGridRightOffset();
        return GetGridInteractableCell(centerCell.GridPosition + rightOffset * directionSign);
    }

    private bool TryConvertLateralCell(TerrainCell centerCell, int directionSign)
    {
        TerrainCell lateralCell = GetLateralCell(centerCell, directionSign);
        if (lateralCell != null && NeedsFarmConversion(lateralCell))
        {
            bool converted = TryConvertToFarmWithCultivateEffect(lateralCell, true);
            if (converted)
                BroadcastTerrainCellStateFromMaster(lateralCell);

            return converted;
        }

        return false;
    }

    private bool TryConvertToFarmWithCultivateEffect(TerrainCell cell, bool spawnCultivateEffect)
    {
        cell = GetInteractableCell(cell);
        if (cell == null)
            return false;

        bool converted = cell.TryConvertToFarm();
        if (converted && spawnCultivateEffect)
        {
            SpawnCultivateGroundEffect(cell);
        }

        return converted;
    }

    private void SpawnCultivateGroundEffect(TerrainCell cell)
    {
        if (_cultivateGroundEffectPrefab == null || cell == null)
            return;

        Vector3 spawnPosition = cell.transform.position;
        if (TryGetCultivateGroundEffectSurfaceY(cell, out float surfaceY))
        {
            spawnPosition.y = surfaceY + _cultivateGroundEffectSurfaceOffset;
        }

        GameObject effect = Instantiate(_cultivateGroundEffectPrefab, spawnPosition, Quaternion.identity);
        Destroy(effect, Mathf.Max(0.1f, _cultivateGroundEffectLifetime));
    }

    private void PlayCultivateLegendarySpinEffect()
    {
        PlayLegendaryCultivateSfx();

        if (_cultivateLegendarySpinEffect == null)
            return;

        _cultivateLegendarySpinHideTween?.Kill();
        _cultivateLegendarySpinHideTween = null;

        SetCultivateLegendarySpinEffectActive(true, true);
    }

    private void StopCultivateLegendarySpinEffect()
    {
        if (_cultivateLegendarySpinEffect == null)
            return;

        _cultivateLegendarySpinHideTween?.Kill();
        SetCultivateLegendarySpinEffectActive(false, false);

        _cultivateLegendarySpinHideTween = DOVirtual.DelayedCall(
            Mathf.Max(0.05f, _cultivateLegendarySpinFadeOutDuration),
            () =>
            {
                SetCultivateLegendarySpinEffectActive(false, true);
                _cultivateLegendarySpinHideTween = null;
            });
    }

    private void SetCultivateLegendarySpinEffectActive(bool active, bool clearParticles)
    {
        if (_cultivateLegendarySpinEffect == null)
            return;

        foreach (ParticleSystem particleSystem in _cultivateLegendarySpinParticles)
        {
            if (active)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Clear(true);
                particleSystem.Play(true);
            }
            else
            {
                ParticleSystemStopBehavior stopBehavior = clearParticles
                    ? ParticleSystemStopBehavior.StopEmittingAndClear
                    : ParticleSystemStopBehavior.StopEmitting;
                particleSystem.Stop(true, stopBehavior);
                if (clearParticles)
                    particleSystem.Clear(true);
            }
        }

        if (active)
        {
            _cultivateLegendarySpinEffect.gameObject.SetActive(true);
        }
        else if (clearParticles)
        {
            _cultivateLegendarySpinEffect.gameObject.SetActive(false);
        }
    }

    private bool TryGetCultivateGroundEffectSurfaceY(TerrainCell cell, out float surfaceY)
    {
        surfaceY = cell.transform.position.y;

        if (cell.FarmTile != null && cell.FarmTile.gameObject.activeInHierarchy &&
            TryGetTopSurfaceY(cell.FarmTile.gameObject, out surfaceY))
        {
            return true;
        }

        return TryGetTopSurfaceY(cell.gameObject, out surfaceY);
    }

    private bool TryGetTopSurfaceY(GameObject target, out float topY)
    {
        topY = 0f;
        if (target == null)
            return false;

        Collider collider = target.GetComponentInChildren<Collider>(true);
        if (collider != null)
        {
            topY = collider.bounds.max.y;
            return true;
        }

        Renderer renderer = target.GetComponentInChildren<Renderer>(true);
        if (renderer != null)
        {
            topY = renderer.bounds.max.y;
            return true;
        }

        return false;
    }

    private Vector3Int GetGridRightOffset()
    {
        if (_owner.PlayerOwner == null)
            return Vector3Int.right;

        Vector3 right = _owner.PlayerOwner.transform.right;
        return new Vector3Int(Mathf.RoundToInt(right.x), 0, Mathf.RoundToInt(right.z));
    }

    private void PlantPlan(List<SowPlantPlan> plantPlan)
    {
        if (plantPlan == null)
            return;

        foreach (SowPlantPlan plan in plantPlan)
        {
            if (plan?.Tile != null && IsTileReadyForSeedPlanting(plan.Tile))
                PlantSeed(plan.Tile, plan.Seed);
        }
    }

    private IEnumerator PlantAfterDelay(FarmTile farmTile, SeedItemDataSO seed)
    {
        yield return new WaitForSeconds(_sowDelay);
        if (farmTile == null)
            yield break;

        if (!IsTileReadyForSeedPlanting(farmTile))
            yield break;

        PlantSeed(farmTile, seed);
    }

    private void PlantSeed(FarmTile farmTile, SeedItemDataSO seed)
    {
        if (farmTile == null || seed == null)
            return;

        if (_owner.IsMine && !TryConsumeSeedForPlanting(ref seed))
            return;

        if (_owner.IsMine)
            _seedSelector?.TrySwitchNextSeed();

        farmTile.PlantSeed(seed);
        _anySeedPlanted = true;
        BroadcastFarmTileStateFromMaster(farmTile);
    }

    private bool TryConsumeSeedForPlanting(ref SeedItemDataSO seed)
    {
        if (seed == null)
            return false;

        if (ConsumeSeed(seed))
            return true;

        if (!EnsureSeedReadyForInteraction(showNoSeedMessage: false))
            return false;

        SeedItemDataSO fallbackSeed = _seedSelector?.SelectedSeed;
        if (fallbackSeed == null || !ConsumeSeed(fallbackSeed))
            return false;

        seed = fallbackSeed;
        return true;
    }

    private void StartSecondaryFallbacks(bool requireOpenFallback)
    {
        CancelSecondaryFallbacks();

        if (requireOpenFallback)
        {
            float openDelay = Mathf.Max(0.05f, _secondaryOpenFallbackDelay);
            _secondaryOpenFallbackCoroutine = StartCoroutine(SecondaryOpenFallbackCoroutine(openDelay));
        }

        float completeDelay = Mathf.Max(0.5f, _secondaryCompleteFallbackDelay);
        _secondaryCompleteFallbackCoroutine = StartCoroutine(SecondaryCompleteFallbackCoroutine(completeDelay));
    }

    private IEnumerator SecondaryOpenFallbackCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_isSecondaryActing && !_secondaryOpened)
            SowOpen();
    }

    private IEnumerator SecondaryCompleteFallbackCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_isSecondaryActing)
            CompleteSecondaryAction();
    }

    private void StopSecondaryOpenFallback()
    {
        if (_secondaryOpenFallbackCoroutine == null)
            return;

        StopCoroutine(_secondaryOpenFallbackCoroutine);
        _secondaryOpenFallbackCoroutine = null;
    }

    private void CancelSecondaryFallbacks()
    {
        if (_secondaryOpenFallbackCoroutine != null)
        {
            StopCoroutine(_secondaryOpenFallbackCoroutine);
            _secondaryOpenFallbackCoroutine = null;
        }

        if (_secondaryCompleteFallbackCoroutine != null)
        {
            StopCoroutine(_secondaryCompleteFallbackCoroutine);
            _secondaryCompleteFallbackCoroutine = null;
        }
    }

    private bool HasAvailableSelectedSeed()
    {
        return _seedSelector != null && _seedSelector.HasSelectedSeedAvailable;
    }

    public void NotifySecondaryInteractBlocked(TerrainCell cell)
    {
        if (PhotonNetwork.IsConnected && !_owner.IsMine)
            return;

        cell = GetInteractableCell(cell);
        if (cell == null)
            return;
        if (IsSowBlockedByDungeon())
            return;
        if (!HasSowableTarget(cell, _owner != null ? _owner.Grade.CurrentGrade : EHelperGrade.Normal))
            return;

        if (!EnsureSeedReadyForInteraction(showNoSeedMessage: true))
            return;
    }

    private bool HasSowableTarget(TerrainCell centerCell, EHelperGrade grade)
    {
        return GetSowPlantTargets(centerCell, grade).Count > 0;
    }

    private bool IsSowBlockedByDungeon()
    {
        return MapManager.Instance != null && MapManager.Instance.IsDungeon;
    }

    private static bool IsTileReadyForSeedPlanting(FarmTile farmTile)
    {
        return farmTile != null
            && !farmTile.HasSeed
            && farmTile.StateMachine != null
            && farmTile.StateMachine.CurrentStateType == EFarmTileStateType.FarmDry;
    }

    private bool EnsureSeedReadyForInteraction(bool showNoSeedMessage)
    {
        if (HasAvailableSelectedSeed())
        {
            if (CanCurrentHelperPlantSeed(_seedSelector.SelectedSeed))
                return true;

            if (showNoSeedMessage)
                ShowUnavailableSeedGradeMessage();

            return false;
        }

        if (PhotonNetwork.IsConnected && !_owner.IsMine)
            return false;

        bool selected = false;
        if (_seedSelector != null)
        {
            selected = _seedSelector.SelectedSeed == null
                ? _seedSelector.TryAutoSelectSeed()
                : _seedSelector.TrySwitchNextSeed();
        }

        if (selected)
        {
            if (CanCurrentHelperPlantSeed(_seedSelector.SelectedSeed))
                return true;

            if (showNoSeedMessage)
                ShowUnavailableSeedGradeMessage();

            return false;
        }

        if (showNoSeedMessage)
            ShowNoSeedItemMessage();

        return false;
    }

    private bool CanCurrentHelperPlantSeed(SeedItemDataSO seed)
    {
        if (seed == null || _owner == null)
            return false;

        return (int)seed.SeedGrade <= (int)_owner.Grade.CurrentGrade;
    }

    private bool ConsumeSeed(SeedItemDataSO seed)
    {
        PlayerInventoryAbility inventory = _owner.PlayerOwner?.GetAbility<PlayerInventoryAbility>();
        if (inventory == null)
            return false;

        return inventory.RemoveItem(seed, 1);
    }

    private void PlayNormalCultivateSfx()
    {
        PlayCultivateSfx(AssetKey.SFX.SowNormalCultivate);
    }

    private void PlayEpicCultivateSfx()
    {
        if (Time.time - _lastEpicCultivateSfxTime < Mathf.Max(0f, _epicCultivateSfxMinInterval))
            return;

        _lastEpicCultivateSfxTime = Time.time;
        PlayCultivateSfx(AssetKey.SFX.SowEpicCultivate);
    }

    private void PlayLegendaryCultivateSfx()
    {
        PlayCultivateSfx(AssetKey.SFX.SowLegendaryCultivate);
    }

    private void PlayCultivateSfx(string clipKey)
    {
        if (_owner == null || SoundManager.Instance == null || string.IsNullOrEmpty(clipKey))
            return;

        SoundManager.Instance.PlaySfx(new SfxPlayRequest(
            clipKey: clipKey,
            spatialMode: ESpatialMode.Positional3D,
            position: _owner.transform.position));
    }

    private static void PlaySowNormalImpactSfx(FarmTile farmTile)
    {
        if (farmTile == null || SoundManager.Instance == null)
            return;

        Vector3 targetPos = farmTile.CropSpawnPoint != null
            ? farmTile.CropSpawnPoint.position
            : farmTile.transform.position;

        SoundManager.Instance.PlaySfx(new SfxPlayRequest(
            clipKey: AssetKey.SFX.SowNormal,
            spatialMode: ESpatialMode.Positional3D,
            position: targetPos));
    }

    private static void PlaySowLegendaryImpactSfx(FarmTile farmTile)
    {
        if (farmTile == null || SoundManager.Instance == null)
            return;

        Vector3 targetPos = farmTile.CropSpawnPoint != null
            ? farmTile.CropSpawnPoint.position
            : farmTile.transform.position;

        SoundManager.Instance.PlaySfx(new SfxPlayRequest(
            clipKey: AssetKey.SFX.SowLegendary,
            spatialMode: ESpatialMode.Positional3D,
            position: targetPos));
    }

    private void CompleteSecondaryAction()
    {
        if (!_isSecondaryActing)
            return;

        CancelSecondaryFallbacks();
        _secondaryPresentation?.CancelPresentation();

        if (_anySeedPlanted)
            _owner.Experience.Add(_sowExperience);

        if (_owner.IsMine && _showUnavailableSeedGradeMessageOnSecondaryComplete)
            ShowUnavailableSeedGradeMessage();
        else if (_owner.IsMine && _showNoSeedMessageOnSecondaryComplete)
            ShowNoSeedItemMessage();

        _animAbility?.Play(EHelperAnim.Idle);
        _currentPlantPlan = null;
        _isSecondaryActing = false;
        _anySeedPlanted = false;
        _secondaryOpened = false;
        _showNoSeedMessageOnSecondaryComplete = false;
        _showUnavailableSeedGradeMessageOnSecondaryComplete = false;
        _owner.EndAction();
    }

    private void CancelCurrentAction()
    {
        CancelSecondaryFallbacks();
        _secondaryPresentation?.CancelPresentation();

        _currentPlantPlan = null;
        _isSecondaryActing = false;
        _anySeedPlanted = false;
        _secondaryOpened = false;
        _showNoSeedMessageOnSecondaryComplete = false;
        _showUnavailableSeedGradeMessageOnSecondaryComplete = false;
    }

    private void OnDisable()
    {
        _cultivateLegendarySpinHideTween?.Kill();
        _cultivateLegendarySpinHideTween = null;
        SetCultivateLegendarySpinEffectActive(false, true);
        StopAllCoroutines();
        CancelCurrentAction();
        _owner?.transform.DOKill();
        _owner?.EndAction();
    }

    private static void BroadcastTerrainCellStateFromMaster(TerrainCell cell)
    {
        if (!PhotonNetwork.IsMasterClient || cell == null) return;
        MapSyncManager.Instance?.BroadcastTerrainCellStateFromMaster(cell.GridPosition);
    }

    private static void BroadcastFarmTileStateFromMaster(FarmTile farmTile)
    {
        if (farmTile == null) return;
        BroadcastTerrainCellStateFromMaster(farmTile.GetComponentInParent<TerrainCell>());
    }

    private static void ShowNoSeedItemMessage()
    {
        if (HarvestNotificationManager.Instance != null)
        {
            HarvestNotificationManager.Instance.ShowMessage(NoSeedItemMessage);
            return;
        }

        Debug.Log(NoSeedItemMessage);
    }

    private void ShowUnavailableSeedGradeMessage()
    {
        if (PhotonNetwork.IsConnected && _owner != null && !_owner.IsMine)
            return;

        if (HarvestNotificationManager.Instance != null)
        {
            HarvestNotificationManager.Instance.ShowMessage(UnavailableSeedGradeMessage);
            return;
        }

        UI_UnableActionText.Show(UnavailableSeedGradeMessage);
    }
}
