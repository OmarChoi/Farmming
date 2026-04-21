using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// 벌목 채굴: 좌클릭(벌목) / 우클릭(채굴)
public enum EGatherType
{
    Wood,
    Stone,
}

public class WoodCuttingMineActionAbility : HelperAbility, IHelperAction, IPrimaryInteractBlockNotifier, ISecondaryInteractBlockNotifier
{
    private const string UnableTreeGatherMessage = "\uC544\uC9C1 \uCC44\uC9D1\uD560 \uC218 \uC5C6\uB294 \uB098\uBB34\uC785\uB2C8\uB2E4.";
    private const string UnableStoneGatherMessage = "\uC544\uC9C1 \uCC44\uC9D1\uD560 \uC218 \uC5C6\uB294 \uB3CC\uC785\uB2C8\uB2E4.";
    private const float UnableTreeGatherFadeDuration = 1.5f;

    [SerializeField] protected GameObject _effectWoodPrefab;
    [SerializeField] private GameObject _woodNormalEffect;
    [SerializeField] private GameObject _woodNormalRangeEffect;
    [SerializeField] private GameObject _woodEpicRangeEffect;
    [SerializeField] private GameObject _woodLegendaryRangeEffect;
    [SerializeField] private GameObject _woodLegendaryRangeEffectGround;
    [SerializeField] protected GameObject _effectStonePrefab;
    [SerializeField] protected Transform _effectSpawnPoint;
    [SerializeField] private Transform _mouthPoint;
    [SerializeField] private float _idleTransition = 0.5f;
    [SerializeField] private float _slashMagicHeightOffset = 1f;
    [SerializeField] private float _slashMagicLifetime = 1f;
    [SerializeField] private float _normalWoodEffectLaunchDelay = 0.3f;
    [SerializeField] private float _normalWoodEffectParticleStartDelay = 0.1f;
    [SerializeField] private float _normalWoodRangeEffectDelay = 0.2f;
    [SerializeField] private float _normalWoodEffectTravelDuration = 0.2f;
    [SerializeField] private float _epicWoodImpactDelay = 0.2f;
    [SerializeField] private Vector3 _slashMagicRotationOffset = Vector3.zero;

    [SerializeField] private float _wideGatherOffset = 2f;

    [SerializeField] private Transform _embeddedNormalWoodEffect;
    [SerializeField] private Transform _embeddedEpicWoodEffect;
    [SerializeField] private Transform _embeddedLegendaryWoodEffect;

    private StoneMineAbility _stoneMineAbility;
    private HelperAnimationAbility _animAbility;
    private RangeBoostEffect _rangeBoostEffect;
    private Vector3 _embeddedNormalWoodEffectLocalPosition;
    private Quaternion _embeddedNormalWoodEffectLocalRotation;
    private Vector3 _embeddedNormalWoodEffectLocalScale;
    private Tween _embeddedNormalWoodEffectActivateTween;
    private Tween _embeddedNormalWoodEffectTween;
    private Tween _embeddedNormalWoodEffectRangeTween;
    private Tween _embeddedNormalWoodEffectResetTween;
    private Vector3 _embeddedEpicWoodEffectLocalPosition;
    private Quaternion _embeddedEpicWoodEffectLocalRotation;
    private Vector3 _embeddedEpicWoodEffectLocalScale;
    private Tween _embeddedEpicWoodEffectResetTween;
    private Vector3 _embeddedLegendaryWoodEffectLocalPosition;
    private Quaternion _embeddedLegendaryWoodEffectLocalRotation;
    private Vector3 _embeddedLegendaryWoodEffectLocalScale;
    private Tween _embeddedLegendaryWoodEffectResetTween;
    private readonly Dictionary<ItemDataSO, int> _pendingGatherNotificationAmounts = new Dictionary<ItemDataSO, int>();
    private Coroutine _gatherNotificationFlushCoroutine;

    protected override void Awake()
    {
        base.Awake();
        _stoneMineAbility = _owner.GetAbility<StoneMineAbility>();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
        _rangeBoostEffect = _owner.GetAbility<RangeBoostEffect>();
        CacheEmbeddedNormalWoodEffect();
        CacheEmbeddedEpicWoodEffect();
        CacheEmbeddedLegendaryWoodEffect();
    }

    private void OnEnable()
    {
        GatheringObject.OnGatheringItemAdded += HandleGatheringItemAdded;
    }

    private void OnDisable()
    {
        GatheringObject.OnGatheringItemAdded -= HandleGatheringItemAdded;
        _pendingGatherNotificationAmounts.Clear();
        _gatherNotificationFlushCoroutine = null;

        StopAllCoroutines();
        ResetEmbeddedNormalWoodEffectTransform(false);
        ResetEmbeddedEpicWoodEffectTransform(false);
        ResetEmbeddedLegendaryWoodEffectTransform(false);
        _owner?.EndAction();
    }

    public bool CanInteractPrimary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return false;
        if (cell.Data.ObjectType != EGridObjectType.Tree) return false;
        if (IsTreeGradeBlocked(cell)) return false;
        return true;
    }

    public void NotifyPrimaryInteractBlocked(TerrainCell cell)
    {
        if (_owner != null && !_owner.IsMine)
            return;

        cell = GetInteractableCell(cell);
        if (!IsTreeGradeBlocked(cell))
            return;

        UI_UnableActionText.Show(UnableTreeGatherMessage, UnableTreeGatherFadeDuration);
    }

    public bool CanInteractSecondary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return false;
        if (cell.Data.ObjectType != EGridObjectType.Rock) return false;
        if (IsStoneGradeBlocked(cell)) return false;
        return true;
    }

    public void NotifySecondaryInteractBlocked(TerrainCell cell)
    {
        if (_owner != null && !_owner.IsMine)
            return;

        cell = GetInteractableCell(cell);
        if (!IsStoneGradeBlocked(cell))
            return;

        UI_UnableActionText.Show(UnableStoneGatherMessage, UnableTreeGatherFadeDuration);
    }

    public void InteractPrimary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return;
        if (IsTreeGradeBlocked(cell)) return;

        _owner.BeginAction();
        EHelperAnim cuttingAnim = _owner.Grade.CurrentGrade switch
        {
            EHelperGrade.Epic => EHelperAnim.WoodEpicCutting,
            EHelperGrade.Legendary => EHelperAnim.WoodLegendaryCutting,
            _ => EHelperAnim.WoodCutting
        };
        _animAbility?.Play(cuttingAnim);

        GatheringInfo info = new GatheringInfo(_owner);

        bool isWideActive = _rangeBoostEffect != null && _rangeBoostEffect.IsActive;
        if (HasNormalWoodEffect())
        {
            if (_owner.Grade.CurrentGrade == EHelperGrade.Normal)
            {
                TerrainCell[] targetCells = GetNormalWoodTargetCells(cell, isWideActive);
                bool shouldSpawnRangeImpactEffect = isWideActive;
                bool preferEmbeddedEffect = !isWideActive;

                foreach (TerrainCell targetCell in targetCells)
                {
                    LaunchNormalWoodEffect(targetCell, info, shouldSpawnRangeImpactEffect, preferEmbeddedEffect);
                }
            }
            else
            {
                SpawnSlashMagicAndGather(cell, info);
            }

            if (isWideActive)
            {
                TerrainCell[] wideCells = GetHorizontalAdjacentCells(cell);
                foreach (TerrainCell wideCell in wideCells)
                {
                    if (_owner.Grade.CurrentGrade != EHelperGrade.Normal)
                    {
                        SpawnSlashMagicAndGather(wideCell, info);
                    }
                }
            }
        }
        else if (_owner.Grade.CurrentGrade == EHelperGrade.Epic)
        {
            TerrainCell[] targetCells = GetEpicWoodTargetCells(cell, isWideActive);
            bool preferEmbeddedEffect = !isWideActive && targetCells.Length == 1;

            bool spawnRangeImpactEffect = isWideActive;
            foreach (TerrainCell targetCell in targetCells)
            {
                LaunchEpicWoodEffect(targetCell, info, preferEmbeddedEffect, spawnRangeImpactEffect);
            }
        }
        else if (_owner.Grade.CurrentGrade == EHelperGrade.Legendary)
        {
            TerrainCell[] targetCells = GetEpicWoodTargetCells(cell, isWideActive);
            bool preferEmbeddedEffect = !isWideActive && targetCells.Length == 1;
            bool spawnRangeImpactEffect = isWideActive;

            foreach (TerrainCell targetCell in targetCells)
            {
                LaunchLegendaryWoodEffect(targetCell, info, preferEmbeddedEffect, spawnRangeImpactEffect);
            }
        }
        else if (isWideActive)
        {
            Vector3 playerRight = _owner.PlayerOwner.transform.right;
            SpawnWoodVFX(info, Vector3.zero);                         
            SpawnWoodVFX(info, playerRight * _wideGatherOffset);  
            SpawnWoodVFX(info, -playerRight * _wideGatherOffset);   
        }
        else
        {
            SpawnWoodVFX(info, Vector3.zero);
        }

        StartCoroutine(AnimPlayCoroutine());
    }

    private void LaunchNormalWoodEffect(TerrainCell cell, GatheringInfo info, bool spawnRangeImpactEffect, bool preferEmbeddedEffect)
    {
        if (cell == null || !HasNormalWoodEffect())
            return;

        Vector3 startPosition = GetWoodEffectSpawnPosition();
        if (startPosition == Vector3.positiveInfinity)
        {
            SpawnSlashMagicAndGather(cell, info);
            return;
        }

        Vector3 targetPosition = GetNormalWoodImpactPosition(cell, startPosition);
        Quaternion spawnRotation = GetNormalWoodEffectSpawnRotation(startPosition, targetPosition);

        if (preferEmbeddedEffect && _embeddedNormalWoodEffect != null)
        {
            LaunchEmbeddedNormalWoodEffect(cell, info, startPosition, spawnRangeImpactEffect);
            return;
        }

        if (spawnRangeImpactEffect)
        {
            float rangeEffectDelay = GetNormalWoodRangeEffectDelay();
            DOVirtual.DelayedCall(rangeEffectDelay, () =>
            {
                SpawnNormalWoodRangeImpactEffect(cell, startPosition);
            });
        }

        float playDelay = GetNormalWoodEffectPlayDelay();
        float impactDelay = GetNormalWoodImpactDelay();
        DOVirtual.DelayedCall(playDelay, () =>
        {
            GameObject effect = SpawnDetachedNormalWoodEffect(startPosition, spawnRotation);
            if (effect == null)
                return;

            if (_slashMagicLifetime > 0f)
                Destroy(effect, _slashMagicLifetime);
            else
                Destroy(effect);
        });

        DOVirtual.DelayedCall(impactDelay, () =>
        {
            TryGatherWoodCell(cell, info);
        });
    }

    private void SpawnSlashMagicAndGather(TerrainCell cell, GatheringInfo info)
    {
        SpawnSlashMagic(cell);
        TryGatherWoodCell(cell, info);
    }

    private void TryGatherWoodCell(TerrainCell cell, GatheringInfo info)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return;
        if (cell.CurrentObject == null) return;
        if (cell.Data.ObjectType != EGridObjectType.Tree) return;

        if (cell.CurrentObject.TryGetComponent<IGatherable>(out IGatherable gatherable) && gatherable is Wood)
        {
            gatherable.TryGather(info);
        }
    }

    private bool IsTreeGradeBlocked(TerrainCell cell)
    {
        if (_owner == null) return false;
        if (!TryGetWood(cell, out Wood wood)) return false;

        GatheringObjectSO gatheringData = wood.GatheringData;
        if (gatheringData == null) return false;

        return _owner.Grade.CurrentGrade < gatheringData.RequiredLevel;
    }

    private bool IsStoneGradeBlocked(TerrainCell cell)
    {
        if (_owner == null) return false;
        if (!TryGetStone(cell, out Stone stone)) return false;

        GatheringObjectSO gatheringData = stone.GatheringData;
        if (gatheringData == null) return false;

        return _owner.Grade.CurrentGrade < gatheringData.RequiredLevel;
    }

    private static bool TryGetWood(TerrainCell cell, out Wood wood)
    {
        wood = null;
        if (cell == null) return false;
        if (cell.CurrentObject == null) return false;
        if (cell.Data.ObjectType != EGridObjectType.Tree) return false;

        if (cell.CurrentObject.TryGetComponent(out wood))
            return true;

        wood = cell.CurrentObject.GetComponentInChildren<Wood>(true);
        return wood != null;
    }

    private static bool TryGetStone(TerrainCell cell, out Stone stone)
    {
        stone = null;
        if (cell == null) return false;
        if (cell.CurrentObject == null) return false;
        if (cell.Data.ObjectType != EGridObjectType.Rock) return false;

        if (cell.CurrentObject.TryGetComponent(out stone))
            return true;

        stone = cell.CurrentObject.GetComponentInChildren<Stone>(true);
        return stone != null;
    }

    private void SpawnWoodVFX(GatheringInfo info, Vector3 worldOffset)
    {
        Vector3 origin = GetWoodEffectSpawnPosition();
        if (origin == Vector3.positiveInfinity)
            origin = _owner.transform.position;

        Quaternion rotation = _mouthPoint != null
            ? _mouthPoint.rotation
            : (_effectSpawnPoint != null ? _effectSpawnPoint.rotation : _owner.transform.rotation);

        Vector3 spawnPos = origin + worldOffset;
        GameObject effect = Instantiate(_effectWoodPrefab, spawnPos, rotation);
        WoodCuttingVFX cuttingVfx = effect.GetComponent<WoodCuttingVFX>();
        cuttingVfx.Initiate(info, EGatherType.Wood);
    }

    private void HandleGatheringItemAdded(GatheringObject gatheringObject, PlayerController player, ItemDataSO item, int amount)
    {
        if (_owner == null || !_owner.IsMine) return;
        if (player == null || player != _owner.PlayerOwner) return;
        if (!_owner.IsActing) return;
        if (item == null || amount <= 0) return;
        if (gatheringObject is not Wood && gatheringObject is not Stone) return;

        if (_pendingGatherNotificationAmounts.TryGetValue(item, out int currentAmount))
            _pendingGatherNotificationAmounts[item] = currentAmount + amount;
        else
            _pendingGatherNotificationAmounts.Add(item, amount);

        if (_gatherNotificationFlushCoroutine != null)
            StopCoroutine(_gatherNotificationFlushCoroutine);

        _gatherNotificationFlushCoroutine = StartCoroutine(FlushGatherNotificationsAfterFrame());
    }

    private IEnumerator FlushGatherNotificationsAfterFrame()
    {
        yield return new WaitForSeconds(0.1f);

        if (_pendingGatherNotificationAmounts.Count <= 0)
        {
            _gatherNotificationFlushCoroutine = null;
            yield break;
        }

        if (HarvestNotificationManager.Instance != null)
        {
            foreach (KeyValuePair<ItemDataSO, int> pair in _pendingGatherNotificationAmounts)
            {
                ItemDataSO item = pair.Key;
                int amount = pair.Value;
                if (item == null || amount <= 0) continue;

                HarvestNotificationManager.Instance.Show(item.Icon, item.DisplayName, amount);
            }
        }

        _pendingGatherNotificationAmounts.Clear();
        _gatherNotificationFlushCoroutine = null;
    }

    private void SpawnSlashMagic(TerrainCell cell)
    {
        if (_woodNormalEffect == null || cell == null) return;

        Vector3 spawnPosition = GetSlashMagicSpawnPosition(cell);
        Quaternion spawnRotation = GetSlashMagicRotation();
        GameObject effect = Instantiate(_woodNormalEffect, spawnPosition, spawnRotation);

        if (_slashMagicLifetime > 0f)
        {
            Destroy(effect, _slashMagicLifetime);
        }
    }

    private Vector3 GetSlashMagicSpawnPosition(TerrainCell cell)
    {
        Vector3 spawnPosition = cell.transform.position + Vector3.up * _slashMagicHeightOffset;

        if (cell.CurrentObject != null)
        {
            Vector3 objectPosition = cell.CurrentObject.transform.position;
            spawnPosition.x = objectPosition.x;
            spawnPosition.z = objectPosition.z;

            Collider collider = cell.CurrentObject.GetComponentInChildren<Collider>();
            if (collider != null)
            {
                spawnPosition.y = Mathf.Max(spawnPosition.y, collider.bounds.center.y);
            }
        }

        return spawnPosition;
    }

    private Vector3 GetNormalWoodImpactPosition(TerrainCell cell, Vector3 originPosition)
    {
        Vector3 fallbackPosition = GetSlashMagicSpawnPosition(cell);

        if (cell == null || cell.CurrentObject == null)
            return fallbackPosition;

        Collider collider = cell.CurrentObject.GetComponentInChildren<Collider>();
        if (collider == null)
            return fallbackPosition;

        Vector3 closestPoint = collider.ClosestPoint(originPosition);
        if ((closestPoint - originPosition).sqrMagnitude <= Mathf.Epsilon)
            return fallbackPosition;

        return closestPoint;
    }

    private void SpawnNormalWoodRangeImpactEffect(TerrainCell cell, Vector3 originPosition)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return;
        if (cell.CurrentObject == null) return;
        if (cell.Data.ObjectType != EGridObjectType.Tree) return;
        if (_woodNormalRangeEffect == null) return;

        Vector3 impactPosition = GetNormalWoodImpactPosition(cell, originPosition);
        Quaternion impactRotation = GetWoodNormalEffectRotation(originPosition, impactPosition);
        GameObject effect = Instantiate(_woodNormalRangeEffect, impactPosition, impactRotation);

        float destroyDelay = Mathf.Max(_slashMagicLifetime, 2f);
        Destroy(effect, destroyDelay);
    }

    private float GetNormalWoodEffectPlayDelay()
    {
        return Mathf.Max(0f, _normalWoodEffectLaunchDelay - _normalWoodEffectParticleStartDelay);
    }

    private float GetNormalWoodImpactDelay()
    {
        return Mathf.Max(0f, _normalWoodEffectLaunchDelay);
    }

    private float GetNormalWoodRangeEffectDelay()
    {
        return Mathf.Max(0f, _normalWoodRangeEffectDelay);
    }

    private TerrainCell[] GetNormalWoodTargetCells(TerrainCell centerCell, bool includeWideCells)
    {
        List<TerrainCell> result = new List<TerrainCell>();

        if (CanInteractPrimary(centerCell))
        {
            result.Add(centerCell);
        }

        if (!includeWideCells)
            return result.ToArray();

        TerrainCell[] wideCells = GetHorizontalAdjacentCells(centerCell);
        foreach (TerrainCell wideCell in wideCells)
        {
            if (CanInteractPrimary(wideCell))
            {
                result.Add(wideCell);
            }
        }

        return result.ToArray();
    }

    private TerrainCell[] GetEpicWoodTargetCells(TerrainCell centerCell, bool includeWideCells)
    {
        List<TerrainCell> result = new List<TerrainCell>();

        if (CanInteractPrimary(centerCell))
        {
            result.Add(centerCell);
        }

        if (!includeWideCells)
            return result.ToArray();

        TerrainCell[] wideCells = GetHorizontalAdjacentCells(centerCell);
        foreach (TerrainCell wideCell in wideCells)
        {
            if (CanInteractPrimary(wideCell))
            {
                result.Add(wideCell);
            }
        }

        return result.ToArray();
    }

    private Vector3 GetWoodEffectSpawnPosition()
    {
        if (_embeddedNormalWoodEffect != null)
            return _embeddedNormalWoodEffect.position;

        if (_mouthPoint != null)
            return _mouthPoint.position;

        if (_effectSpawnPoint != null)
            return _effectSpawnPoint.position;

        return Vector3.positiveInfinity;
    }

    private Vector3 GetEpicWoodEffectSpawnPosition()
    {
        if (_embeddedEpicWoodEffect != null)
            return _embeddedEpicWoodEffect.position;

        if (_mouthPoint != null)
            return _mouthPoint.position;

        if (_effectSpawnPoint != null)
            return _effectSpawnPoint.position;

        return Vector3.positiveInfinity;
    }

    private bool HasNormalWoodEffect()
    {
        return _embeddedNormalWoodEffect != null || _woodNormalEffect != null;
    }

    private void CacheEmbeddedNormalWoodEffect()
    {
        if (_embeddedNormalWoodEffect == null)
            return;

        _embeddedNormalWoodEffectLocalPosition = _embeddedNormalWoodEffect.localPosition;
        _embeddedNormalWoodEffectLocalRotation = _embeddedNormalWoodEffect.localRotation;
        _embeddedNormalWoodEffectLocalScale = _embeddedNormalWoodEffect.localScale;
        _embeddedNormalWoodEffect.gameObject.SetActive(false);
    }

    private void CacheEmbeddedEpicWoodEffect()
    {
        if (_embeddedEpicWoodEffect == null)
            return;

        _embeddedEpicWoodEffectLocalPosition = _embeddedEpicWoodEffect.localPosition;
        _embeddedEpicWoodEffectLocalRotation = _embeddedEpicWoodEffect.localRotation;
        _embeddedEpicWoodEffectLocalScale = _embeddedEpicWoodEffect.localScale;
        _embeddedEpicWoodEffect.gameObject.SetActive(false);
    }

    private void CacheEmbeddedLegendaryWoodEffect()
    {
        if (_embeddedLegendaryWoodEffect == null)
            return;

        _embeddedLegendaryWoodEffectLocalPosition = _embeddedLegendaryWoodEffect.localPosition;
        _embeddedLegendaryWoodEffectLocalRotation = _embeddedLegendaryWoodEffect.localRotation;
        _embeddedLegendaryWoodEffectLocalScale = _embeddedLegendaryWoodEffect.localScale;
        _embeddedLegendaryWoodEffect.gameObject.SetActive(false);
    }

    private GameObject SpawnDetachedNormalWoodEffect(Vector3 startPosition, Quaternion spawnRotation)
    {
        GameObject source = _embeddedNormalWoodEffect != null
            ? _embeddedNormalWoodEffect.gameObject
            : _woodNormalEffect;

        if (source == null)
            return null;

        GameObject effect = Instantiate(source, startPosition, spawnRotation);
        effect.SetActive(true);
        RestartParticleSystems(effect.transform);
        return effect;
    }

    private GameObject SpawnDetachedEpicWoodEffect(Vector3 startPosition, Quaternion spawnRotation)
    {
        GameObject source = _embeddedEpicWoodEffect != null
            ? _embeddedEpicWoodEffect.gameObject
            : null;

        if (source == null)
            return null;

        GameObject effect = Instantiate(source, startPosition, spawnRotation);
        effect.SetActive(true);
        RestartParticleSystems(effect.transform);
        return effect;
    }

    private GameObject SpawnDetachedLegendaryWoodEffect(Vector3 startPosition, Quaternion spawnRotation)
    {
        GameObject source = _embeddedLegendaryWoodEffect != null
            ? _embeddedLegendaryWoodEffect.gameObject
            : null;

        if (source == null)
            return null;

        GameObject effect = Instantiate(source, startPosition, spawnRotation);
        effect.SetActive(true);
        RestartParticleSystems(effect.transform);
        return effect;
    }

    private void LaunchEpicWoodEffect(TerrainCell cell, GatheringInfo info, bool preferEmbeddedEffect, bool spawnRangeImpactEffect)
    {
        if (cell == null)
            return;

        Vector3 startPosition = GetEpicWoodEffectSpawnPosition();
        if (startPosition == Vector3.positiveInfinity)
        {
            TryGatherWoodCell(cell, info);
            return;
        }

        Vector3 targetPosition = GetNormalWoodImpactPosition(cell, startPosition);
        Quaternion spawnRotation = GetEpicWoodEffectSpawnRotation(startPosition, targetPosition);

        if (preferEmbeddedEffect && _embeddedEpicWoodEffect != null)
        {
            PlayEmbeddedEpicWoodEffect(startPosition, targetPosition);
        }
        else
        {
            GameObject effect = SpawnDetachedEpicWoodEffect(startPosition, spawnRotation);
            if (effect != null)
            {
                Destroy(effect, GetEpicWoodEffectLifetime());
            }
        }

        DOVirtual.DelayedCall(Mathf.Max(0f, _epicWoodImpactDelay), () =>
        {
            if (spawnRangeImpactEffect)
            {
                SpawnEpicWoodRangeImpactEffect(cell, startPosition);
            }

            TryGatherWoodCell(cell, info);
        });
    }

    private void LaunchLegendaryWoodEffect(TerrainCell cell, GatheringInfo info, bool preferEmbeddedEffect, bool spawnRangeImpactEffect)
    {
        if (cell == null)
            return;

        Vector3 startPosition = GetLegendaryWoodEffectSpawnPosition();
        if (startPosition == Vector3.positiveInfinity || _embeddedLegendaryWoodEffect == null)
        {
            SpawnWoodVFX(info, Vector3.zero);
            return;
        }

        Vector3 targetPosition = GetNormalWoodImpactPosition(cell, startPosition);
        Quaternion spawnRotation = GetLegendaryWoodEffectSpawnRotation(startPosition, targetPosition);

        if (preferEmbeddedEffect && _embeddedLegendaryWoodEffect != null)
        {
            PlayEmbeddedLegendaryWoodEffect(startPosition, targetPosition);
        }
        else
        {
            GameObject effect = SpawnDetachedLegendaryWoodEffect(startPosition, spawnRotation);
            if (effect != null)
            {
                Destroy(effect, GetLegendaryWoodEffectLifetime());
            }
        }

        DOVirtual.DelayedCall(Mathf.Max(0f, _epicWoodImpactDelay), () =>
        {
            if (spawnRangeImpactEffect)
            {
                SpawnLegendaryWoodRangeImpactEffects(cell, startPosition);
            }

            TryGatherWoodCell(cell, info);
        });
    }

    private void SpawnEpicWoodRangeImpactEffect(TerrainCell cell, Vector3 originPosition)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return;
        if (cell.CurrentObject == null) return;
        if (cell.Data.ObjectType != EGridObjectType.Tree) return;
        if (_woodEpicRangeEffect == null) return;

        Vector3 impactPosition = GetNormalWoodImpactPosition(cell, originPosition);
        Quaternion impactRotation = GetEpicWoodEffectSpawnRotation(originPosition, impactPosition);
        GameObject effect = Instantiate(_woodEpicRangeEffect, impactPosition, impactRotation);

        float destroyDelay = Mathf.Max(GetEpicWoodEffectLifetime(), 2f);
        Destroy(effect, destroyDelay);
    }

    private void SpawnLegendaryWoodRangeImpactEffects(TerrainCell cell, Vector3 originPosition)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return;
        if (cell.CurrentObject == null) return;
        if (cell.Data.ObjectType != EGridObjectType.Tree) return;

        float destroyDelay = Mathf.Max(GetLegendaryWoodEffectLifetime(), 2f);

        if (_woodLegendaryRangeEffect != null)
        {
            Vector3 impactPosition = GetNormalWoodImpactPosition(cell, originPosition);
            Quaternion impactRotation = GetLegendaryWoodEffectSpawnRotation(originPosition, impactPosition);
            GameObject impactEffect = Instantiate(_woodLegendaryRangeEffect, impactPosition, impactRotation);
            Destroy(impactEffect, destroyDelay);
        }

        if (_woodLegendaryRangeEffectGround != null)
        {
            Vector3 groundPosition = cell.transform.position;
            Quaternion groundRotation = GetLegendaryWoodEffectSpawnRotation(originPosition, groundPosition);
            GameObject groundEffect = Instantiate(_woodLegendaryRangeEffectGround, groundPosition, groundRotation);
            Destroy(groundEffect, destroyDelay);
        }
    }

    private void LaunchEmbeddedNormalWoodEffect(TerrainCell cell, GatheringInfo info, Vector3 originPosition, bool spawnRangeImpactEffect)
    {
        ResetEmbeddedNormalWoodEffectTransform(false);

        if (_embeddedNormalWoodEffect == null)
            return;

        if (spawnRangeImpactEffect)
        {
            float rangeEffectDelay = GetNormalWoodRangeEffectDelay();
            _embeddedNormalWoodEffectRangeTween = DOVirtual.DelayedCall(rangeEffectDelay, () =>
            {
                SpawnNormalWoodRangeImpactEffect(cell, originPosition);
            });
        }

        float playDelay = GetNormalWoodEffectPlayDelay();
        _embeddedNormalWoodEffectActivateTween = DOVirtual.DelayedCall(playDelay, () =>
        {
            if (_embeddedNormalWoodEffect == null)
                return;

            _embeddedNormalWoodEffect.gameObject.SetActive(true);
            RestartEmbeddedNormalWoodEffectParticles();
        });

        float impactDelay = GetNormalWoodImpactDelay();
        _embeddedNormalWoodEffectTween = DOVirtual.DelayedCall(impactDelay, () =>
        {
            TryGatherWoodCell(cell, info);

            float resetDelay = Mathf.Max(0f, _slashMagicLifetime);
            _embeddedNormalWoodEffectResetTween = DOVirtual.DelayedCall(resetDelay, () =>
            {
                ResetEmbeddedNormalWoodEffectTransform(false);
            });
        });
    }

    private void RestartEmbeddedNormalWoodEffectParticles()
    {
        if (_embeddedNormalWoodEffect == null)
            return;

        RestartParticleSystems(_embeddedNormalWoodEffect);
    }

    private void RestartParticleSystems(Transform root)
    {
        if (root == null)
            return;

        ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    private void ResetEmbeddedNormalWoodEffectTransform(bool keepActive)
    {
        _embeddedNormalWoodEffectActivateTween?.Kill();
        _embeddedNormalWoodEffectActivateTween = null;

        _embeddedNormalWoodEffectTween?.Kill();
        _embeddedNormalWoodEffectTween = null;

        _embeddedNormalWoodEffectRangeTween?.Kill();
        _embeddedNormalWoodEffectRangeTween = null;

        _embeddedNormalWoodEffectResetTween?.Kill();
        _embeddedNormalWoodEffectResetTween = null;

        if (_embeddedNormalWoodEffect == null)
            return;

        _embeddedNormalWoodEffect.SetParent(_mouthPoint, false);
        _embeddedNormalWoodEffect.localPosition = _embeddedNormalWoodEffectLocalPosition;
        _embeddedNormalWoodEffect.localRotation = _embeddedNormalWoodEffectLocalRotation;
        _embeddedNormalWoodEffect.localScale = _embeddedNormalWoodEffectLocalScale;

        if (!keepActive)
            _embeddedNormalWoodEffect.gameObject.SetActive(false);
    }

    private void PlayEmbeddedEpicWoodEffect(Vector3 startPosition, Vector3 targetPosition)
    {
        if (_owner == null || _owner.Grade.CurrentGrade != EHelperGrade.Epic)
            return;

        ResetEmbeddedEpicWoodEffectTransform(false);

        if (_embeddedEpicWoodEffect == null)
            return;

        _embeddedEpicWoodEffect.rotation = GetEpicWoodEffectSpawnRotation(startPosition, targetPosition);
        _embeddedEpicWoodEffect.gameObject.SetActive(true);
        RestartParticleSystems(_embeddedEpicWoodEffect);

        float resetDelay = GetEpicWoodEffectLifetime();
        _embeddedEpicWoodEffectResetTween = DOVirtual.DelayedCall(resetDelay, () =>
        {
            ResetEmbeddedEpicWoodEffectTransform(false);
        });
    }

    private void PlayEmbeddedLegendaryWoodEffect(Vector3 startPosition, Vector3 targetPosition)
    {
        if (_owner == null || _owner.Grade.CurrentGrade != EHelperGrade.Legendary)
            return;

        ResetEmbeddedLegendaryWoodEffectTransform(false);

        if (_embeddedLegendaryWoodEffect == null)
            return;

        _embeddedLegendaryWoodEffect.position = startPosition;
        _embeddedLegendaryWoodEffect.rotation = GetLegendaryWoodEffectSpawnRotation(startPosition, targetPosition);
        _embeddedLegendaryWoodEffect.gameObject.SetActive(true);
        RestartParticleSystems(_embeddedLegendaryWoodEffect);

        float resetDelay = GetLegendaryWoodEffectLifetime();
        _embeddedLegendaryWoodEffectResetTween = DOVirtual.DelayedCall(resetDelay, () =>
        {
            ResetEmbeddedLegendaryWoodEffectTransform(false);
        });
    }

    private void ResetEmbeddedEpicWoodEffectTransform(bool keepActive)
    {
        _embeddedEpicWoodEffectResetTween?.Kill();
        _embeddedEpicWoodEffectResetTween = null;

        if (_embeddedEpicWoodEffect == null)
            return;

        _embeddedEpicWoodEffect.SetParent(_mouthPoint, false);
        _embeddedEpicWoodEffect.localPosition = _embeddedEpicWoodEffectLocalPosition;
        _embeddedEpicWoodEffect.localRotation = _embeddedEpicWoodEffectLocalRotation;
        _embeddedEpicWoodEffect.localScale = _embeddedEpicWoodEffectLocalScale;

        if (!keepActive)
            _embeddedEpicWoodEffect.gameObject.SetActive(false);
    }

    private void ResetEmbeddedLegendaryWoodEffectTransform(bool keepActive)
    {
        _embeddedLegendaryWoodEffectResetTween?.Kill();
        _embeddedLegendaryWoodEffectResetTween = null;

        if (_embeddedLegendaryWoodEffect == null)
            return;

        _embeddedLegendaryWoodEffect.localPosition = _embeddedLegendaryWoodEffectLocalPosition;
        _embeddedLegendaryWoodEffect.localRotation = _embeddedLegendaryWoodEffectLocalRotation;
        _embeddedLegendaryWoodEffect.localScale = _embeddedLegendaryWoodEffectLocalScale;

        if (!keepActive)
            _embeddedLegendaryWoodEffect.gameObject.SetActive(false);
    }

    private Quaternion GetSlashMagicRotation()
    {
        Vector3 forward = _owner?.PlayerOwner != null ? _owner.PlayerOwner.transform.forward : Vector3.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= Mathf.Epsilon)
        {
            forward = Vector3.forward;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up) * Quaternion.Euler(_slashMagicRotationOffset);
    }

    private Quaternion GetWoodNormalEffectRotation(Vector3 startPosition, Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - startPosition;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return GetSlashMagicRotation();

        return Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Euler(_slashMagicRotationOffset);
    }

    private Quaternion GetEpicWoodEffectSpawnRotation(Vector3 startPosition, Vector3 targetPosition)
    {
        Quaternion templateRotation = _embeddedEpicWoodEffect != null
            ? _embeddedEpicWoodEffect.rotation
            : (_mouthPoint != null
                ? _mouthPoint.rotation
                : (_effectSpawnPoint != null ? _effectSpawnPoint.rotation : GetSlashMagicRotation()));

        Vector3 templateForward = Vector3.ProjectOnPlane(templateRotation * Vector3.forward, Vector3.up);
        if (templateForward.sqrMagnitude <= Mathf.Epsilon)
        {
            templateForward = Vector3.ProjectOnPlane(_owner?.PlayerOwner != null ? _owner.PlayerOwner.transform.forward : Vector3.forward, Vector3.up);
        }

        Vector3 targetDirection = Vector3.ProjectOnPlane(targetPosition - startPosition, Vector3.up);
        if (targetDirection.sqrMagnitude <= Mathf.Epsilon || templateForward.sqrMagnitude <= Mathf.Epsilon)
        {
            return templateRotation;
        }

        Quaternion yawDelta = Quaternion.FromToRotation(templateForward.normalized, targetDirection.normalized);
        return yawDelta * templateRotation;
    }

    private Vector3 GetLegendaryWoodEffectSpawnPosition()
    {
        if (_mouthPoint != null)
            return _mouthPoint.position;

        if (_embeddedLegendaryWoodEffect != null)
            return _embeddedLegendaryWoodEffect.position;

        if (_effectSpawnPoint != null)
            return _effectSpawnPoint.position;

        return Vector3.positiveInfinity;
    }

    private Quaternion GetLegendaryWoodEffectSpawnRotation(Vector3 startPosition, Vector3 targetPosition)
    {
        Quaternion templateRotation = _embeddedLegendaryWoodEffect != null
            ? _embeddedLegendaryWoodEffect.rotation
            : (_mouthPoint != null
                ? _mouthPoint.rotation
                : (_effectSpawnPoint != null ? _effectSpawnPoint.rotation : GetSlashMagicRotation()));

        Vector3 templateForward = Vector3.ProjectOnPlane(templateRotation * Vector3.forward, Vector3.up);
        if (templateForward.sqrMagnitude <= Mathf.Epsilon)
        {
            templateForward = Vector3.ProjectOnPlane(_owner?.PlayerOwner != null ? _owner.PlayerOwner.transform.forward : Vector3.forward, Vector3.up);
        }

        Vector3 targetDirection = Vector3.ProjectOnPlane(targetPosition - startPosition, Vector3.up);
        if (targetDirection.sqrMagnitude <= Mathf.Epsilon || templateForward.sqrMagnitude <= Mathf.Epsilon)
        {
            return templateRotation;
        }

        Quaternion yawDelta = Quaternion.FromToRotation(templateForward.normalized, targetDirection.normalized);
        return yawDelta * templateRotation;
    }

    private float GetEpicWoodEffectLifetime()
    {
        return Mathf.Max(2f, _slashMagicLifetime);
    }

    private float GetLegendaryWoodEffectLifetime()
    {
        return Mathf.Max(2f, _slashMagicLifetime);
    }

    private Quaternion GetNormalWoodEffectSpawnRotation(Vector3 startPosition, Vector3 targetPosition)
    {
        Quaternion templateRotation = _embeddedNormalWoodEffect != null
            ? _embeddedNormalWoodEffect.rotation
            : (_mouthPoint != null
                ? _mouthPoint.rotation
                : (_effectSpawnPoint != null ? _effectSpawnPoint.rotation : GetSlashMagicRotation()));

        Vector3 templateForward = Vector3.ProjectOnPlane(templateRotation * Vector3.forward, Vector3.up);
        if (templateForward.sqrMagnitude <= Mathf.Epsilon)
        {
            templateForward = Vector3.ProjectOnPlane(_owner?.PlayerOwner != null ? _owner.PlayerOwner.transform.forward : Vector3.forward, Vector3.up);
        }

        Vector3 targetDirection = Vector3.ProjectOnPlane(targetPosition - startPosition, Vector3.up);
        if (targetDirection.sqrMagnitude <= Mathf.Epsilon || templateForward.sqrMagnitude <= Mathf.Epsilon)
        {
            return templateRotation;
        }

        Quaternion yawDelta = Quaternion.FromToRotation(templateForward.normalized, targetDirection.normalized);
        return yawDelta * templateRotation;
    }

    private IEnumerator AnimPlayCoroutine()
    {
        yield return new WaitForSeconds(_idleTransition);
        _animAbility?.Play(EHelperAnim.Idle);
        _owner.EndAction();
    }

    public void InteractSecondary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return;
        if (IsStoneGradeBlocked(cell)) return;

        if (cell != null && cell.CurrentObject != null && cell.Data.ObjectType == EGridObjectType.Tree)
            return;

        _owner.BeginAction();
        _stoneMineAbility?.JumpAndSmash(cell);
    }

}
