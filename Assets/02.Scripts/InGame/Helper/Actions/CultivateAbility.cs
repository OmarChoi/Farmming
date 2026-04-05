using DG.Tweening;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;
using System.Collections;
using System;

public class CultivateAbility : HelperAbility
{
    [SerializeField] private GameObject _effectDustPrefab;
    [SerializeField] private Transform _effectSpawnPoint;
    [SerializeField] private float _jumpHeight = 2f;
    [SerializeField] private float _jumpDuration = 0.5f;
    [SerializeField] private float _returnDuration = 0.5f;
    [SerializeField] private float _endEffect = 0.2f;
    [SerializeField] private float _cultivateDuration = 0.5f;
    [SerializeField] private float _rejumpHeight = 0.5f;
    [SerializeField] private int _jumpCount = 1;

    [SerializeField] private float _rayOriginHeight = 3f;
    [SerializeField] private float _rayDistance = 5f;

    [Header("Epic 방향 전환")]
    [SerializeField] private float _epicInitialWait = 0.2f;
    [SerializeField] private float _epicLookDuration = 0.8f;

    [Header("Legendary 스핀")]
    [SerializeField] private float _spinDuration = 2f;
    [SerializeField] private float _spinDegreesPerSecond = 720f;

    private HelperAnimationAbility _animAbility;
    private bool _isJumping = false;

    private void Start()
    {
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
    }

    private void OnDisable()
    {
        _isJumping = false;
        _owner?.EndAction();

        _owner?.transform.DOKill();
    }

    public void JumpAndCultivate(
        TerrainCell cell,
        System.Action onCultivate = null,
        bool spin = false,
        bool epicLook = false,
        System.Action onEpicLookLeft = null,
        System.Action onEpicLookRight = null)
        bool epicLookLeft = true,
        bool epicLookRight = true,
        Action onEpicLookLeft = null,
        Action onEpicLookRight = null,
        float epicRightLookDuration = 0f,
        Action onEpicLookRightMid = null)
    {
        if(_isJumping)
        {
            return;
        }

        Vector3 targetPos = GetTerrainLandPosition(cell);
        StartCoroutine(JumpCoroutine(targetPos, onCultivate, spin, epicLook, epicLookLeft, epicLookRight, onEpicLookLeft, onEpicLookRight, epicRightLookDuration, onEpicLookRightMid));
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

    private IEnumerator JumpCoroutine(
        Vector3 targetPosition,
        Action onCultivate = null,
        bool spin = false,
        bool epicLook = false,
        bool epicLookLeft = true,
        bool epicLookRight = true,
        Action onEpicLookLeft = null,
        Action onEpicLookRight = null,
        float epicRightLookDuration = 0f,
        Action onEpicLookRightMid = null)
    {
        _isJumping = true;

        Transform parentBackup = _owner.transform.parent;
        _owner.transform.SetParent(null);

        _animAbility.Play(EHelperAnim.Jump);
        yield return Move(_owner.transform, targetPosition, _jumpHeight, _jumpDuration);

        _animAbility.Play(EHelperAnim.Cultivate);
        SpawnDustEffect();
        onCultivate?.Invoke();

        if (epicLook && _owner.PlayerOwner != null)
        {
            yield return new WaitForSeconds(_epicInitialWait);

            Vector3 rightDir = _owner.PlayerOwner.transform.right;
            Quaternion leftRotation  = Quaternion.LookRotation(-rightDir, Vector3.up);
            Quaternion rightRotation = Quaternion.LookRotation(rightDir,  Vector3.up);

            if (epicLookLeft)
            {
                yield return _owner.transform
                    .DORotateQuaternion(leftRotation, _epicLookDuration)
                    .SetEase(Ease.InOutQuad)
                    .WaitForCompletion();
                onEpicLookLeft?.Invoke();
            }

            if (epicLookRight)
            {
                float rightDuration = epicRightLookDuration > 0f ? epicRightLookDuration : _epicLookDuration;
                var seq = DOTween.Sequence()
                    .Append(_owner.transform.DORotateQuaternion(rightRotation, rightDuration).SetEase(Ease.InOutQuad));
                if (onEpicLookRightMid != null)
                    seq.InsertCallback(rightDuration * 0.5f, () => onEpicLookRightMid.Invoke());
                yield return seq.WaitForCompletion();
                onEpicLookRight?.Invoke();
            }
        }
        else
        {
            yield return new WaitForSeconds(_cultivateDuration);
        }

        if (spin)
        {
            float elapsed = 0f;
            while (elapsed < _spinDuration)
            {
                _owner.transform.Rotate(Vector3.up, _spinDegreesPerSecond * Time.deltaTime, Space.World);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

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

    private void SpawnDustEffect()
    {
        if (_effectDustPrefab == null) return;
        Vector3 spawnPos = _effectSpawnPoint != null ? _effectSpawnPoint.position : _owner.transform.position;
        GameObject effect = Instantiate(_effectDustPrefab, spawnPos, Quaternion.identity);
        Destroy(effect, _endEffect);
    }
}
