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

public class WoodCuttingMineActionAbility : HelperAbility, IHelperAction
{
    [SerializeField] protected GameObject _effectWoodPrefab;
    [SerializeField] private GameObject _woodNormalEffect;
    [SerializeField] private GameObject _woodNormalRangeEffect;
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
    [SerializeField] private Vector3 _slashMagicRotationOffset = Vector3.zero;

    [SerializeField] private float _wideGatherOffset = 2f;

    private StoneMineAbility _stoneMineAbility;
    private HelperAnimationAbility _animAbility;
    private RangeBoostEffect _rangeBoostEffect;
    private Transform _embeddedNormalWoodEffect;
    private Vector3 _embeddedNormalWoodEffectLocalPosition;
    private Quaternion _embeddedNormalWoodEffectLocalRotation;
    private Vector3 _embeddedNormalWoodEffectLocalScale;
    private Tween _embeddedNormalWoodEffectActivateTween;
    private Tween _embeddedNormalWoodEffectTween;
    private Tween _embeddedNormalWoodEffectRangeTween;
    private Tween _embeddedNormalWoodEffectResetTween;

    protected override void Awake()
    {
        base.Awake();
        _stoneMineAbility = _owner.GetAbility<StoneMineAbility>();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
        _rangeBoostEffect = _owner.GetAbility<RangeBoostEffect>();
        CacheEmbeddedNormalWoodEffect();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ResetEmbeddedNormalWoodEffectTransform(false);
        _owner?.EndAction();
    }

    public bool CanInteractPrimary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return false;
        return cell.Data.ObjectType == EGridObjectType.Tree;
    }

    public bool CanInteractSecondary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return false;
        return cell.Data.ObjectType == EGridObjectType.Rock;
    }

    public void InteractPrimary(TerrainCell cell)
    {
        cell = GetInteractableCell(cell);
        if (cell == null) return;

        _owner.BeginAction();
        _animAbility?.Play(EHelperAnim.WoodCutting);

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

    private bool HasNormalWoodEffect()
    {
        return _embeddedNormalWoodEffect != null || _woodNormalEffect != null;
    }

    private void CacheEmbeddedNormalWoodEffect()
    {
        _embeddedNormalWoodEffect = FindEmbeddedNormalWoodEffect();
        if (_embeddedNormalWoodEffect == null)
            return;

        _embeddedNormalWoodEffectLocalPosition = _embeddedNormalWoodEffect.localPosition;
        _embeddedNormalWoodEffectLocalRotation = _embeddedNormalWoodEffect.localRotation;
        _embeddedNormalWoodEffectLocalScale = _embeddedNormalWoodEffect.localScale;
        _embeddedNormalWoodEffect.gameObject.SetActive(false);
    }

    private Transform FindEmbeddedNormalWoodEffect()
    {
        if (_mouthPoint == null)
            return null;

        for (int i = 0; i < _mouthPoint.childCount; i++)
        {
            Transform child = _mouthPoint.GetChild(i);
            if (child == null)
                continue;

            if (child.GetComponentInChildren<ParticleSystem>(true) != null)
                return child;
        }

        return null;
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

    private TerrainCell[] GetHorizontalAdjacentCells(TerrainCell centerCell)
    {
        if (centerCell == null) return new TerrainCell[0];
        if (TerrainGridManager.Instance == null) return new TerrainCell[0];
        if (_owner?.PlayerOwner == null) return new TerrainCell[0];

        Vector3 playerRight = _owner.PlayerOwner.transform.right;
        Vector3Int rightOffset = new Vector3Int(
            Mathf.RoundToInt(playerRight.x),
            0,
            Mathf.RoundToInt(playerRight.z));

        if (rightOffset == Vector3Int.zero)
            rightOffset = Vector3Int.right;

        Vector3Int centerGrid = centerCell.GridPosition;
        List<TerrainCell> result = new List<TerrainCell>();

        TerrainCell rightCell = TerrainGridManager.Instance.GetInteractionCell(centerGrid + rightOffset, false, out _);
        TerrainCell leftCell = TerrainGridManager.Instance.GetInteractionCell(centerGrid - rightOffset, false, out _);

        if (rightCell != null) result.Add(rightCell);
        if (leftCell != null) result.Add(leftCell);

        return result.ToArray();
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

        if (cell != null && cell.CurrentObject != null && cell.Data.ObjectType == EGridObjectType.Tree)
            return;

        _owner.BeginAction();
        _stoneMineAbility?.JumpAndSmash(cell);
    }

    private TerrainCell GetInteractableCell(TerrainCell cell)
    {
        if (TerrainGridManager.Instance == null) return null;
        return TerrainGridManager.Instance.IsCellAvailableForInteraction(cell) ? cell : null;
    }
}
