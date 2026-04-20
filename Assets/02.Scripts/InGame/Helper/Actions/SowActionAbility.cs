using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Photon.Pun;
using UnityEngine;

public class SowActionAbility : HelperAbility, IHelperAction, ISecondaryInteractBlockNotifier
{
    private const string NoSeedItemMessage = "씨앗 아이템이 없습니다";

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
    [SerializeField] private float _epicCultivateSfxMinInterval = 0.15f;

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
    private List<FarmTile> _currentFarmTiles;
    private SeedItemDataSO _currentSeed;
    private Coroutine _secondaryOpenFallbackCoroutine;
    private Coroutine _secondaryCompleteFallbackCoroutine;
    private bool _secondaryOpened;
    private float _lastEpicCultivateSfxTime = -999f;

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
        return CanSow(cell) && EnsureSeedReadyForInteraction(showNoSeedMessage: false);
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

        SeedItemDataSO selectedSeed = _seedSelector?.SelectedSeed;
        if (selectedSeed == null || !HasAvailableSelectedSeed())
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
        if (!_isSecondaryActing || _secondaryOpened)
            return;

        _secondaryOpened = true;
        StopSecondaryOpenFallback();

        if (_currentFarmTiles == null || _currentFarmTiles.Count == 0 || _currentSeed == null || !_seedVfxSpawner.HasMouthPoint)
            return;

        foreach (FarmTile tile in _currentFarmTiles)
        {
            FarmTile capturedTile = tile;
            _seedVfxSpawner.SpawnTo(capturedTile, () => PlaySowNormalImpactSfx(capturedTile));
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
            case EHelperGrade.Legendary:
                StartGradeSecondary(centerCell, seed, grade);
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
        _secondaryOpened = false;

        _owner.BeginAction();
        StartSecondaryFallbacks(requireOpenFallback: true);
        _animAbility?.Play(EHelperAnim.Sow);
    }

    private void StartGradeSecondary(TerrainCell centerCell, SeedItemDataSO seed, EHelperGrade grade)
    {
        _isSecondaryActing = true;
        _anySeedPlanted = false;
        _currentFarmTiles = null;
        _currentSeed = null;
        _secondaryOpened = false;

        _owner.BeginAction();
        StartSecondaryFallbacks(requireOpenFallback: false);

        if (_secondaryPresentation == null)
        {
            PlantCells(GetTargetCells(centerCell), seed);
            CompleteSecondaryAction();
            return;
        }

        switch (grade)
        {
            case EHelperGrade.Epic:
                _secondaryPresentation.PlayEpic(
                    _seedVfxSpawner.MouthPoint,
                    GetEpicOrderedTargetCells(centerCell),
                    cell =>
                    {
                        FarmTile tile = GetFarmTile(cell);
                        if (tile != null && tile.IsReadyToSow)
                        {
                            PlaySowNormalImpactSfx(tile);
                            PlantSeed(tile, seed);
                        }
                    },
                    CompleteSecondaryAction);
                break;
            case EHelperGrade.Legendary:
                bool playedLegendaryImpactSfx = false;
                _secondaryPresentation.PlayLegendary(
                    _seedVfxSpawner.MouthPoint,
                    GetOrderedTargetCells(centerCell),
                    cell =>
                    {
                        FarmTile tile = GetFarmTile(cell);
                        if (tile != null && tile.IsReadyToSow)
                        {
                            if (!playedLegendaryImpactSfx)
                            {
                                PlaySowLegendaryImpactSfx(tile);
                                playedLegendaryImpactSfx = true;
                            }
                            PlantSeed(tile, seed);
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
                    PlayEpicCultivateSfx();
                    if (TryConvertToFarmWithCultivateEffect(leftCell, true))
                    {
                        _owner.Experience.Add(_cultivateExperience);
                        BroadcastTerrainCellStateFromMaster(leftCell);
                    }
                    ReplayEpicSow(false);
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
                    PlayEpicCultivateSfx();
                    if (TryConvertToFarmWithCultivateEffect(cell, true))
                        _owner.Experience.Add(_cultivateExperience);
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
                    _owner.Experience.Add(_cultivateExperience);
                BroadcastTerrainCellStateFromMaster(cell);
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
        return farmTile != null && farmTile.IsReadyToSow;
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

    private TerrainCell GetLateralCell(TerrainCell centerCell, int directionSign)
    {
        Vector3Int rightOffset = GetGridRightOffset();
        return GetGridInteractableCell(centerCell.GridPosition + rightOffset * directionSign);
    }

    private void TryConvertLateralCell(TerrainCell centerCell, int directionSign)
    {
        TerrainCell lateralCell = GetLateralCell(centerCell, directionSign);
        if (lateralCell != null && NeedsFarmConversion(lateralCell))
        {
            TryConvertToFarmWithCultivateEffect(lateralCell, true);
            BroadcastTerrainCellStateFromMaster(lateralCell);
        }
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
        if (farmTile == null || seed == null)
            return;
        if (_owner.IsMine && !ConsumeSeed(seed))
            return;
        if (_owner.IsMine)
            _seedSelector?.TrySwitchNextSeed();

        farmTile.PlantSeed(seed);
        _anySeedPlanted = true;
        BroadcastFarmTileStateFromMaster(farmTile);
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
        if (cell == null || !CanSow(cell))
            return;

        EnsureSeedReadyForInteraction(showNoSeedMessage: true);
    }

    private bool EnsureSeedReadyForInteraction(bool showNoSeedMessage)
    {
        if (HasAvailableSelectedSeed())
            return true;

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
            return true;

        if (showNoSeedMessage)
            ShowNoSeedItemMessage();

        return false;
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

        _animAbility?.Play(EHelperAnim.Idle);
        _currentFarmTiles = null;
        _currentSeed = null;
        _isSecondaryActing = false;
        _anySeedPlanted = false;
        _secondaryOpened = false;
        _owner.EndAction();
    }

    private void CancelCurrentAction()
    {
        CancelSecondaryFallbacks();
        _secondaryPresentation?.CancelPresentation();

        _currentFarmTiles = null;
        _currentSeed = null;
        _isSecondaryActing = false;
        _anySeedPlanted = false;
        _secondaryOpened = false;
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
}
