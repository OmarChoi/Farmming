using System.Collections;
using DG.Tweening;
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
    private bool _isJumping = false;

    private void Start()
    {
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
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

            if(cell.CurrentObject.TryGetComponent<IGatherable>(out IGatherable gatherable))
            {
                Vector3 targetPos = GetLandPosition(cell.CurrentObject);
                StartCoroutine(JumpCoroutine(targetPos, gatherable));
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

    private IEnumerator JumpCoroutine(Vector3 targetPosition, IGatherable gatherable = null)
    {
        _isJumping = true;

        Transform parentBackup = _owner.transform.parent;
        _owner.transform.SetParent(null);

        Vector3 endPosition = targetPosition;

        _animAbility.Play(EHelperAnim.Jump);
        yield return Move(_owner.transform, endPosition, _jumpHeight, _jumpDuration);

        _animAbility.Play(EHelperAnim.Stun);
        SpawnStoneEffect();
        gatherable?.TryGather(new GatheringInfo(_owner));

        yield return new WaitForSeconds(_stunDuration);

        Vector3 returnPosition = parentBackup != null ? parentBackup.position : _owner.PlayerOwner.transform.position;

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
}
