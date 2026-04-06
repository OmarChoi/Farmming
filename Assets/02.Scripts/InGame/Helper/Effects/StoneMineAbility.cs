using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneMineAbility : HelperAbility
{
    [SerializeField] private GameObject _effectStonePrefab;
    [SerializeField] private Transform _effectSpawnPoint;
    [SerializeField] private float _jumpHeight = 2f;
    [SerializeField] private float _jumpDuration = 0.5f;
    [SerializeField] private float _returnDuration = 0.5f;
    [SerializeField] private float _endEffect = 0.2f;
    [SerializeField] private float _stunDuration = 0.5f;
    [SerializeField] private float _rejumpHeight = 0.5f;
    [SerializeField] private float _rotationDuration = 0.2f;
    [SerializeField] private float _headOffset = 0.5f;
    [SerializeField] private int _jumpCount = 1;

    [SerializeField] private float _rayOriginHeight = 3f;
    [SerializeField] private float _rayDistance = 5f;

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

                StartCoroutine(JumpCoroutine(targetPos, gatherable, wideCells));
            }
        }
        else
        {
            Vector3 targetPos = GetTerrainLandPosition(cell);
            StartCoroutine(JumpCoroutine(targetPos));
        }
    }

    private TerrainCell[] GetHorizontalAdjacentCells(TerrainCell centerCell)
    {
        if (TerrainGridManager.Instance == null) return new TerrainCell[0];

        Vector3 playerRight = _owner.PlayerOwner.transform.right;
        Vector3Int rightOffset = SnapToGridAxis(playerRight);

        Vector3Int centerGrid = centerCell.GridPosition;
        var result = new List<TerrainCell>();

        var rightCell = TerrainGridManager.Instance.GetCell(centerGrid + rightOffset);
        var leftCell = TerrainGridManager.Instance.GetCell(centerGrid - rightOffset);

        if (rightCell != null) result.Add(rightCell);
        if (leftCell != null) result.Add(leftCell);

        return result.ToArray();
    }

    private Vector3Int SnapToGridAxis(Vector3 direction)
    {
        float absX = Mathf.Abs(direction.x);
        float absZ = Mathf.Abs(direction.z);

        if (absX >= absZ)
            return new Vector3Int(direction.x > 0 ? 1 : -1, 0, 0);
        else
            return new Vector3Int(0, 0, direction.z > 0 ? 1 : -1);
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

    private IEnumerator JumpCoroutine(Vector3 targetPosition, IGatherable gatherable = null, TerrainCell[] wideCells = null)
    {
        _isJumping = true;

        Transform parentBackup = _owner.transform.parent;
        _owner.transform.SetParent(null);

        _animAbility.Play(EHelperAnim.Jump);
        yield return Move(_owner.transform, targetPosition, _jumpHeight, _jumpDuration);

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
                    SpawnStoneEffectAt(wideCell.transform.position);
                    wideGatherable.TryGather(new GatheringInfo(_owner));
                }
            }
        }

        yield return new WaitForSeconds(_stunDuration);

        Vector3 returnPosition = parentBackup != null
            ? parentBackup.position
            : _owner.PlayerOwner.transform.position;

        _animAbility.Play(EHelperAnim.Jump);
        yield return Move(_owner.transform, returnPosition, _jumpHeight * _rejumpHeight, _returnDuration);

        _owner.transform.SetParent(parentBackup);
        _owner.transform.localPosition = Vector3.zero;
        _owner.transform.localRotation = Quaternion.identity;

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

    private void SpawnStoneEffect()
    {
        if (_effectStonePrefab == null) return;
        Vector3 spawnPos = _effectSpawnPoint != null ? _effectSpawnPoint.position : _owner.transform.position;
        GameObject effect = Instantiate(_effectStonePrefab, spawnPos, Quaternion.identity);
        Destroy(effect, _endEffect);
    }

    private void SpawnStoneEffectAt(Vector3 worldPosition)
    {
        if (_effectStonePrefab == null) return;
        GameObject effect = Instantiate(_effectStonePrefab, worldPosition, Quaternion.identity);
        Destroy(effect, _endEffect);
    }
}
