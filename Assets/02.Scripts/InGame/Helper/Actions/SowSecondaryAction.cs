using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class SowSecondaryActionConfig
{
    public float SowDelay;
    public int SowExperience;
    public float FloatAbovePlayerHeight;
    public float FloatMoveDuration;
    public float FloatDuration;
    public float BobAmplitude;
    public float BobDuration;
    public float ReturnDuration;
    public float VfxTriggerNormalizedTime;
}

public sealed class SowSecondaryAction
{
    private readonly MonoBehaviour _coroutineHost;
    private readonly HelperController _owner;
    private readonly HelperAnimationAbility _animAbility;
    private readonly SowEpicSecondaryVFXAbility _epicSecondaryVFX;
    private readonly SowLegendarySecondaryVFXAbility _legendarySecondaryVFX;
    private readonly SowTargetResolver _targetResolver;
    private readonly SowSeedVfxSpawner _seedVfxSpawner;
    private readonly SowSecondaryActionConfig _config;
    private readonly Func<IEnumerator> _replayEpicSowAndWait;

    private bool _anySeedPlanted;
    private List<FarmTile> _currentFarmTiles;
    private SeedItemDataSO _currentSeed;

    public bool IsActing { get; private set; }

    public SowSecondaryAction(
        MonoBehaviour coroutineHost,
        HelperController owner,
        HelperAnimationAbility animAbility,
        SowEpicSecondaryVFXAbility epicSecondaryVFX,
        SowLegendarySecondaryVFXAbility legendarySecondaryVFX,
        SowTargetResolver targetResolver,
        SowSeedVfxSpawner seedVfxSpawner,
        SowSecondaryActionConfig config,
        Func<IEnumerator> replayEpicSowAndWait)
    {
        _coroutineHost = coroutineHost;
        _owner = owner;
        _animAbility = animAbility;
        _epicSecondaryVFX = epicSecondaryVFX;
        _legendarySecondaryVFX = legendarySecondaryVFX;
        _targetResolver = targetResolver;
        _seedVfxSpawner = seedVfxSpawner;
        _config = config;
        _replayEpicSowAndWait = replayEpicSowAndWait;
    }

    public IEnumerator FloatAndSow(TerrainCell cell, SeedItemDataSO seed, List<FarmTile> farmTiles)
    {
        if (_owner.PlayerOwner == null) yield break;

        IsActing = true;
        _anySeedPlanted = false;
        _owner.BeginAction();

        HelperFollowAbility followAbility = _owner.GetAbility<HelperFollowAbility>();
        if (followAbility != null) followAbility.enabled = false;

        Vector3 originalPos = _owner.transform.position;

        Vector3 floatPos = _owner.PlayerOwner.transform.position + Vector3.up * _config.FloatAbovePlayerHeight;
        yield return _owner.transform.DOMove(floatPos, _config.FloatMoveDuration)
            .SetEase(Ease.OutQuad).WaitForCompletion();

        Tween bobTween = _owner.transform.DOMoveY(floatPos.y + _config.BobAmplitude, _config.BobDuration)
            .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

        yield return new WaitForSeconds(_config.FloatDuration);
        bobTween.Kill();

        if (_owner.Grade.CurrentGrade == EHelperGrade.Legendary)
        {
            _animAbility?.Replay(EHelperAnim.LegendarySow);
            if (_animAbility != null)
                yield return _coroutineHost.StartCoroutine(_animAbility.WaitForNormalizedTime(_config.VfxTriggerNormalizedTime));
        }

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

        yield return _owner.transform.DOMove(originalPos, _config.ReturnDuration)
            .SetEase(Ease.InOutQuad).WaitForCompletion();

        if (followAbility != null) followAbility.enabled = true;
        CompleteAction();
    }

    public void StartNormal(List<FarmTile> farmTiles, SeedItemDataSO seed)
    {
        IsActing = true;
        _anySeedPlanted = false;
        _currentFarmTiles = farmTiles;
        _currentSeed = seed;

        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.Sow);
    }

    public void SowOpen()
    {
        if (_currentFarmTiles == null || _currentFarmTiles.Count == 0 || !_seedVfxSpawner.HasMouthPoint)
        {
            return;
        }

        foreach (FarmTile tile in _currentFarmTiles)
        {
            FarmTile capturedTile = tile;
            _seedVfxSpawner.SpawnTo(capturedTile);
            _coroutineHost.StartCoroutine(PlantAfterDelay(capturedTile, _currentSeed));
        }
    }

    public void SowClose()
    {
        CompleteAction();
    }

    public void Cancel()
    {
        IsActing = false;
        _currentFarmTiles = null;
        _currentSeed = null;
        _anySeedPlanted = false;

        HelperFollowAbility followAbility = _owner?.GetAbility<HelperFollowAbility>();
        if (followAbility != null) followAbility.enabled = true;
    }

    private void SpawnNormalSecondaryVFX(List<FarmTile> farmTiles, SeedItemDataSO seed, Action onComplete)
    {
        foreach (FarmTile tile in farmTiles)
        {
            _seedVfxSpawner.SpawnTo(tile);
            PlantSeed(tile, seed);
        }
        onComplete?.Invoke();
    }

    private void SpawnEpicSecondaryVFX(TerrainCell centerCell, SeedItemDataSO seed, Action onComplete)
    {
        Vector3Int rightOffset = _targetResolver.GetGridRightOffset();
        var orderedCells = new List<TerrainCell>();

        var leftCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition - rightOffset);
        var rightCell = TerrainGridManager.Instance?.GetCell(centerCell.GridPosition + rightOffset);

        if (leftCell != null) orderedCells.Add(leftCell);
        orderedCells.Add(centerCell);
        if (rightCell != null) orderedCells.Add(rightCell);

        if (_epicSecondaryVFX == null)
        {
            PlantCells(orderedCells, seed);
            onComplete?.Invoke();
            return;
        }

        _epicSecondaryVFX.SpawnEffects(_seedVfxSpawner.MouthPoint, orderedCells, cell =>
        {
            FarmTile tile = _targetResolver.GetFarmTile(cell);
            if (tile != null && tile.IsReadyToSow) PlantSeed(tile, seed);
        }, onComplete, _replayEpicSowAndWait);
    }

    private void SpawnLegendarySecondaryVFX(TerrainCell centerCell, SeedItemDataSO seed, Action onComplete)
    {
        List<TerrainCell> orderedCells = _targetResolver.GetOrderedTargetCells(centerCell);

        if (_legendarySecondaryVFX == null)
        {
            PlantCells(orderedCells, seed);
            onComplete?.Invoke();
            return;
        }

        _legendarySecondaryVFX.SpawnEffects(_seedVfxSpawner.MouthPoint, orderedCells, cell =>
        {
            FarmTile tile = _targetResolver.GetFarmTile(cell);
            if (tile != null && tile.IsReadyToSow) PlantSeed(tile, seed);
        }, onComplete);
    }

    private void PlantCells(List<TerrainCell> cells, SeedItemDataSO seed)
    {
        foreach (TerrainCell cell in cells)
        {
            FarmTile tile = _targetResolver.GetFarmTile(cell);
            if (tile != null && tile.IsReadyToSow) PlantSeed(tile, seed);
        }
    }

    private IEnumerator PlantAfterDelay(FarmTile farmTile, SeedItemDataSO seed)
    {
        yield return new WaitForSeconds(_config.SowDelay);
        if (farmTile == null) yield break;

        PlantSeed(farmTile, seed);
    }

    private void PlantSeed(FarmTile farmTile, SeedItemDataSO seed)
    {
        farmTile.PlantSeed(seed);
        _anySeedPlanted = true;
    }

    private void CompleteAction()
    {
        if (_anySeedPlanted) _owner.Experience.Add(_config.SowExperience);
        _animAbility?.Play(EHelperAnim.Idle);
        _currentFarmTiles = null;
        _currentSeed = null;
        IsActing = false;
        _anySeedPlanted = false;
        _owner.EndAction();
    }
}
