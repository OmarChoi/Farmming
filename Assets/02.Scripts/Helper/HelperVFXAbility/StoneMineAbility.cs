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
    [SerializeField] private float _endOffset = 0.3f;
    [SerializeField] private float _endEffect = 0.2f;
    [SerializeField] private float _stunDuration = 0.5f;
    [SerializeField] private float _rejumpHeight = 0.5f;

    private HelperAnimationAbility _animAbility;
    private bool _isJumping = false;

    private void Start()
    {
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
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
                StartCoroutine(JumpCoroutine(cell.CurrentObject.transform.position, gatherable));
            }           
        }
        else
        {
            StartCoroutine(JumpCoroutine(cell.transform.position));
        }
    }

    private IEnumerator JumpCoroutine(Vector3 targetPosition, IGatherable gatherable = null)
    {
        _isJumping=true;

        Vector3 startPosition = transform.position;
        Vector3 endPosition = targetPosition + Vector3.up * _endOffset;

        _animAbility.Play(EHelperAnim.Jump);
        yield return Move(_owner.transform, endPosition, _jumpHeight, _jumpDuration);

        _animAbility.Play(EHelperAnim.Stun);
        SpawnStoneEffect();
        gatherable?.TryGather(_owner.Data.GatherDamage);

        yield return new WaitForSeconds(_stunDuration);

        _animAbility.Play(EHelperAnim.Jump);
        yield return Move(_owner.transform, startPosition, _jumpHeight * _rejumpHeight, _returnDuration);

        _animAbility.Play(EHelperAnim.Idle);
        _isJumping = false;
    }

    private YieldInstruction Move(Transform target, Vector3 to, float height, float duration)
    {
        Vector3 dir = (to - target.position).normalized;
        if (dir != Vector3.zero)
            target.DORotateQuaternion(Quaternion.LookRotation(dir), 0.2f);

        return target.DOJump(to, height, 1, duration)
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
