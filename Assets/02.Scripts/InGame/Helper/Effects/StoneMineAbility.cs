using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneMineAbility : HelperAbility
{
    [SerializeField] private GameObject _effectStonePrefab;
    [SerializeField] private GameObject _targetStoneEffectPrefab;
    [SerializeField] private Transform _effectSpawnPoint;
    [SerializeField] private float _jumpHeight = 2f;
    [SerializeField] private float _epicJumpHeight = 3.2f;
    [SerializeField] private float _legendaryJumpHeight = 3.5f;
    [SerializeField] private float _jumpDuration = 0.5f;
    [SerializeField] private float _returnDuration = 0.5f;
    [SerializeField] private float _rangeBoostScaleMultiplier = 3f;
    [SerializeField] private float _rangeBoostScaleTweenDuration = 0.2f;
    [SerializeField] private float _endEffect = 0.8f;
    [SerializeField] private float _stunDuration = 0.5f;
    [SerializeField] private float _rejumpHeight = 0.5f;
    [SerializeField] private float _rotationDuration = 0.2f;
    [SerializeField] private float _headOffset = 0.5f;
    [SerializeField] private int _jumpCount = 1;

    [SerializeField] private float _rayOriginHeight = 3f;
    [SerializeField] private float _rayDistance = 5f;
    [SerializeField] private float _targetStoneEffectYOffset = 0.15f;
    [SerializeField] private float _epicTargetStoneEffectYOffset = 3.25f;
    [SerializeField] private float _legendaryTargetStoneEffectYOffset = 2.5f;
    [SerializeField] private float _targetStoneEffectHorizontalOffset = 0.35f;
    [SerializeField] private float _targetStoneEffectDuration = 2f;
    [SerializeField] private float _targetStoneEffectSimulationSpeed = 2.5f;
    [SerializeField] private float _cameraShakePerRockMultiplier = 0.35f;

    private HelperAnimationAbility _animAbility;
    private RangeBoostEffect _rangeBoostEffect;
    private bool _isJumping = false;

    private void Start()
    {
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
        _rangeBoostEffect = _owner.GetAbility<RangeBoostEffect>();
    }

    private void OnDisable()
    {
        _isJumping = false;
        _owner?.transform.DOKill();
        _owner?.EndAction();
    }

    public void JumpAndSmash(TerrainCell cell)
    {
        if (TerrainGridManager.Instance != null && !TerrainGridManager.Instance.IsCellAvailableForInteraction(cell))
        {
            return;
        }

        if(_isJumping || cell == null)
        {
            return;
        }

        if(cell.CurrentObject != null)
        {
            if(cell.Data.ObjectType == EGridObjectType.Tree)
            {
                return;
            }

            if (cell.CurrentObject.TryGetComponent<IGatherable>(out IGatherable gatherable))
            {
                Vector3 targetPos = GetLandPosition(cell.CurrentObject);

                TerrainCell[] wideCells = null;
                if (_rangeBoostEffect != null && _rangeBoostEffect.IsActive)
                    wideCells = GetHorizontalAdjacentCells(cell);

                bool isRockTarget = cell.Data.ObjectType == EGridObjectType.Rock;
                Vector3 targetEffectPosition = GetCellSurfaceEffectPosition(cell);
                StartCoroutine(JumpCoroutine(targetPos, gatherable, wideCells, isRockTarget, targetEffectPosition));
            }
        }
        else
        {
            Vector3 targetPos = GetTerrainLandPosition(cell);
            StartCoroutine(JumpCoroutine(targetPos));
        }
    }

    private Vector3 GetLandPosition(GameObject target)
    {
        Collider col = target.GetComponentInChildren<Collider>();
        if (col != null)
        {
            return new Vector3(target.transform.position.x, col.bounds.max.y, target.transform.position.z);
        }

        return target.transform.position;
    }

    private Vector3 GetTerrainLandPosition(TerrainCell cell)
    {
        Vector3 rayOrigin = cell.transform.position + Vector3.up * _rayOriginHeight;
        Ray ray = new Ray(rayOrigin, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, _rayDistance))
        {
            return hit.point;
        }

        return cell.transform.position;
    }

    private Vector3 GetCellSurfaceEffectPosition(TerrainCell cell)
    {
        if (cell == null)
            return Vector3.zero;

        float verticalOffset = GetTargetStoneEffectYOffset();
        Vector3 basePosition = cell.transform.position + Vector3.up * verticalOffset;
        if (_owner?.PlayerOwner == null)
            return basePosition;

        Vector3 toPlayer = _owner.PlayerOwner.transform.position - cell.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude <= Mathf.Epsilon)
            return basePosition;

        Vector3 horizontalOffset = toPlayer.normalized * _targetStoneEffectHorizontalOffset;
        return basePosition + horizontalOffset;
    }

    private float GetTargetStoneEffectYOffset()
    {
        if (_owner == null)
            return _targetStoneEffectYOffset;

        return _owner.Grade.CurrentGrade switch
        {
            EHelperGrade.Epic => _epicTargetStoneEffectYOffset,
            EHelperGrade.Legendary => _legendaryTargetStoneEffectYOffset,
            _ => _targetStoneEffectYOffset,
        };
    }

    private void PlayMiningCameraShake(bool shouldSpawnCenterTargetEffect, TerrainCell[] wideCells)
    {
        if (_owner?.PlayerOwner == null)
            return;

        bool isRangeBoostActive = _rangeBoostEffect != null && _rangeBoostEffect.IsActive;
        if (!isRangeBoostActive && shouldSpawnCenterTargetEffect)
            return;

        PlayerCameraAbility cameraAbility = _owner.PlayerOwner.GetAbility<PlayerCameraAbility>();
        if (cameraAbility == null)
            return;

        int rockHitCount = shouldSpawnCenterTargetEffect ? 1 : 0;

        if (wideCells != null)
        {
            foreach (var wideCell in wideCells)
            {
                if (wideCell == null || wideCell.CurrentObject == null) continue;
                if (wideCell.Data.ObjectType != EGridObjectType.Rock) continue;
                if (!wideCell.CurrentObject.TryGetComponent<IGatherable>(out _)) continue;
                rockHitCount++;
            }
        }

        if (rockHitCount <= 0)
            return;

        float shakeMultiplier = 1f + ((rockHitCount - 1) * _cameraShakePerRockMultiplier);
        cameraAbility.PlayImpactShake(shakeMultiplier);
    }

    private IEnumerator JumpCoroutine(Vector3 targetPosition, IGatherable gatherable = null, TerrainCell[] wideCells = null, bool isRockTarget = false, Vector3? targetEffectPosition = null)
    {
        _isJumping = true;
        bool useWideScale = wideCells != null;
        Vector3 originalLocalScale = _owner.transform.localScale;
        float jumpHeight = GetJumpHeightForCurrentGrade();
        EHelperGrade currentGrade = _owner != null ? _owner.Grade.CurrentGrade : EHelperGrade.Normal;

        Transform parentBackup = _owner.transform.parent;
        _owner.transform.SetParent(null);
        Vector3 detachedBaseScale = _owner.transform.localScale;

        if (useWideScale)
        {
            _owner.transform.DOScale(detachedBaseScale * _rangeBoostScaleMultiplier, _rangeBoostScaleTweenDuration)
                .SetEase(Ease.OutQuad);
        }

        _animAbility.Play(EHelperAnim.Jump);
        yield return Move(_owner.transform, targetPosition, jumpHeight, _jumpDuration);

        bool shouldSpawnCenterTargetEffect = isRockTarget && gatherable != null;
        if ((currentGrade == EHelperGrade.Epic || currentGrade == EHelperGrade.Legendary) && shouldSpawnCenterTargetEffect)
        {
            SpawnStoneEffectAt(targetEffectPosition ?? targetPosition);
        }

        if ((currentGrade == EHelperGrade.Epic || currentGrade == EHelperGrade.Legendary) && wideCells != null)
        {
            foreach (var wideCell in wideCells)
            {
                if (wideCell == null || wideCell.CurrentObject == null) continue;
                if (wideCell.Data.ObjectType != EGridObjectType.Rock) continue;
                if (!wideCell.CurrentObject.TryGetComponent<IGatherable>(out _)) continue;

                SpawnStoneEffectAt(GetCellSurfaceEffectPosition(wideCell));
            }
        }

        if (currentGrade == EHelperGrade.Normal && shouldSpawnCenterTargetEffect)
        {
            SpawnStoneEffectAt(targetEffectPosition ?? targetPosition);
        }
        PlayMiningCameraShake(shouldSpawnCenterTargetEffect, wideCells);
        _animAbility.Play(EHelperAnim.Stun);
        SpawnStoneEffect();
        gatherable?.TryGather(new GatheringInfo(_owner));

        if (wideCells != null)
        {
            foreach (var wideCell in wideCells)
            {
                if (wideCell == null || wideCell.CurrentObject == null) continue;
                if (wideCell.Data.ObjectType == EGridObjectType.Tree) continue;

                if (wideCell.CurrentObject.TryGetComponent<IGatherable>(out IGatherable wideGatherable))
                {
                    wideGatherable.TryGather(new GatheringInfo(_owner));
                }
            }
        }

        yield return new WaitForSeconds(_stunDuration);

        if(parentBackup == null && _owner.PlayerOwner == null)
        {
            if (useWideScale)
                _owner.transform.localScale = detachedBaseScale;

            _isJumping = false;
            _owner.EndAction();
            yield break;
        }

        Vector3 returnPosition = parentBackup != null
            ? parentBackup.position
            : _owner.PlayerOwner.transform.position;

        _animAbility.Play(EHelperAnim.Jump);

        if (useWideScale)
        {
            _owner.transform.DOScale(detachedBaseScale, _returnDuration)
                .SetEase(Ease.InQuad);
        }

        yield return Move(_owner.transform, returnPosition, jumpHeight * _rejumpHeight, _returnDuration);

        _owner.transform.SetParent(parentBackup);
        _owner.transform.localPosition = Vector3.zero;
        _owner.transform.localRotation = Quaternion.identity;
        _owner.transform.localScale = originalLocalScale;

        _animAbility.Play(EHelperAnim.Idle);
        _isJumping = false;
        _owner.EndAction();
    }

    private YieldInstruction Move(Transform target, Vector3 to, float height, float duration)
    {
        return target.DOJump(to, height, _jumpCount, duration)
                     .SetEase(Ease.Linear)
                     .WaitForCompletion();
    }

    private float GetJumpHeightForCurrentGrade()
    {
        if (_owner == null)
            return _jumpHeight;

        return _owner.Grade.CurrentGrade switch
        {
            EHelperGrade.Legendary => _legendaryJumpHeight,
            EHelperGrade.Epic => _epicJumpHeight,
            _ => _jumpHeight
        };
    }

    private void SpawnStoneEffect()
    {
        if (_effectStonePrefab == null) return;
        Vector3 spawnPos = _effectSpawnPoint != null ? _effectSpawnPoint.position : _owner.transform.position;
        GameObject effect = Instantiate(_effectStonePrefab, spawnPos, Quaternion.identity);
        Destroy(effect, _endEffect);
    }

    private void SpawnStoneEffectAt(Vector3 worldPosition)
    {
        GameObject targetEffectPrefab = _targetStoneEffectPrefab != null ? _targetStoneEffectPrefab : _effectStonePrefab;
        if (targetEffectPrefab == null) return;
        GameObject effect = Instantiate(targetEffectPrefab, worldPosition, Quaternion.identity);

        if (_targetStoneEffectSimulationSpeed > 0f)
        {
            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.simulationSpeed = _targetStoneEffectSimulationSpeed;
            }
        }

        Destroy(effect, _targetStoneEffectDuration);
    }
}
