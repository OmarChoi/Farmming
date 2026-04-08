using DG.Tweening;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Tooltip("Sow animation normalized time used to trigger the seed VFX.")]
    [SerializeField] [Range(0f, 1f)] private float _vfxTriggerNormalizedTime = 0.5f;

    private CultivateAbility _cultivateAbility;
    private SeedSelectAbility _seedSelector;
    private HelperAnimationAbility _animAbility;
    private SowEpicSecondaryVFXAbility _epicSecondaryVFX;
    private SowLegendarySecondaryVFXAbility _legendarySecondaryVFX;

    private SowTargetResolver _targetResolver;
    private SowCultivationService _cultivationService;
    private SowSeedVfxSpawner _seedVfxSpawner;
    private SowSecondaryAction _secondaryAction;

    private void Start()
    {
        EnsureServices();
    }

    public bool CanInteractPrimary(TerrainCell cell)
    {
        EnsureServices();
        return _cultivationService.CanInteractPrimary(cell);
    }

    public bool CanInteractSecondary(TerrainCell cell)
    {
        EnsureServices();
        return _targetResolver.CanSow(cell);
    }

    public void InteractPrimary(TerrainCell cell)
    {
        EnsureServices();
        _cultivationService.InteractPrimary(cell);
    }

    public void InteractSecondary(TerrainCell cell)
    {
        EnsureServices();

        if (_owner.IsMine && _secondaryAction.IsActing) return;

        SeedItemDataSO selectedSeed = _seedSelector?.SelectedSeed;
        if (selectedSeed == null) return;

        List<FarmTile> farmTiles = _targetResolver.GetSowableFarmTiles(cell);
        if (farmTiles.Count == 0) return;

        if (_owner.Grade.CurrentGrade == EHelperGrade.Normal)
        {
            _secondaryAction.StartNormal(farmTiles, selectedSeed);
        }
        else
        {
            StartCoroutine(_secondaryAction.FloatAndSow(cell, selectedSeed, farmTiles));
        }

        var pos = cell.GridPosition;
        _owner.PhotonView.RpcSafe(
            nameof(RPC_PlantSeed), RpcTarget.Others,
            pos.x, pos.y, pos.z, selectedSeed.Id);
    }

    public void SowOpen()
    {
        EnsureServices();
        _secondaryAction.SowOpen();
    }

    public void SowClose()
    {
        EnsureServices();
        _secondaryAction.SowClose();
    }

    [PunRPC]
    internal void RPC_PlantSeed(int gridX, int gridY, int gridZ, int seedId)
    {
        EnsureServices();

        var centerCell = TerrainGridManager.Instance?.GetCell(new Vector3Int(gridX, gridY, gridZ));
        if (centerCell == null) return;

        var seed = TerrainGridManager.Instance.SeedDatabase?.GetById(seedId);
        if (seed == null) return;

        List<FarmTile> farmTiles = _targetResolver.GetSowableFarmTiles(centerCell);
        if (farmTiles.Count == 0) return;

        _secondaryAction.StartNormal(farmTiles, seed);
    }

    private IEnumerator ReplayEpicSowAndWait()
    {
        if (_animAbility == null) yield break;
        yield return StartCoroutine(_animAbility.ForceReplayAndWait(EHelperAnim.EpicSow, _vfxTriggerNormalizedTime));
    }

    private void OnDisable()
    {
        _secondaryAction?.Cancel();
        _owner?.transform.DOKill();
        _owner?.EndAction();
    }

    private void EnsureServices()
    {
        if (_targetResolver != null)
        {
            return;
        }

        _cultivateAbility = _owner.GetAbility<CultivateAbility>();
        _seedSelector = _owner.GetAbility<SeedSelectAbility>();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
        _epicSecondaryVFX = _owner.GetAbility<SowEpicSecondaryVFXAbility>();
        _legendarySecondaryVFX = _owner.GetAbility<SowLegendarySecondaryVFXAbility>();

        _targetResolver = new SowTargetResolver(_owner);
        _seedVfxSpawner = new SowSeedVfxSpawner(() => _mouthPoint, () => _seedVfxPrefab);
        _cultivationService = new SowCultivationService(
            _owner,
            _cultivateAbility,
            _targetResolver,
            _cultivateExperience,
            _epicThreeTileLookDuration,
            ReplayEpicSowAndWait,
            routine => StartCoroutine(routine));

        _secondaryAction = new SowSecondaryAction(
            this,
            _owner,
            _animAbility,
            _epicSecondaryVFX,
            _legendarySecondaryVFX,
            _targetResolver,
            _seedVfxSpawner,
            CreateSecondaryActionConfig(),
            ReplayEpicSowAndWait);
    }

    private SowSecondaryActionConfig CreateSecondaryActionConfig()
    {
        return new SowSecondaryActionConfig
        {
            SowDelay = _sowDelay,
            SowExperience = _sowExperience,
            FloatAbovePlayerHeight = _floatAbovePlayerHeight,
            FloatMoveDuration = _floatMoveDuration,
            FloatDuration = _floatDuration,
            BobAmplitude = _bobAmplitude,
            BobDuration = _bobDuration,
            ReturnDuration = _returnDuration,
            VfxTriggerNormalizedTime = _vfxTriggerNormalizedTime
        };
    }
}
