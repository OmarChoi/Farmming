using UnityEngine;

public class HelperFollowAbility : HelperAbility
{
    // TODO : 추후 stat에서 가져오기
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private float _stopDistance = 2.5f;
    [SerializeField] private float _slowDownDistance = 3f;
    [SerializeField] private float _teleportDistance = 8f;
    [SerializeField] private float _teleportHeightDiff = 1.8f;
    [SerializeField] private float _smoothTime = 0.2f;
    [SerializeField] private float _teleportDelay = 1f;
    [SerializeField] private float _runSpeedThreshold = 0.5f;
    [SerializeField] private float _unequipJumpForce = 5f;
    [SerializeField] private float _unequipBackForce = 3f;
    [SerializeField] private float _launchRotationSpeed = 10f;
    [SerializeField] private float _autoJumpForce = 8f;
    [SerializeField] private float _autoJumpCheckDist = 0.5f;
    [SerializeField] private float _autoJumpMaxHeight = 2.2f;
    [SerializeField] private LayerMask _jumpCheckMask = ~0;

    private const float Gravity = 9.8f;
    private const float GroundedYVelocity = -0.5f;

    private CharacterController _cc;
    private HelperAnimationAbility _animAbility;
    private float _yVelocity;
    private float _currentSpeed;
    private float _speedSmoothVelocity;
    private float _teleportTimer;
    private Vector3 _launchVelocity;
    private Quaternion _launchTargetRotation;

    private void Start()
    {
        _cc = _owner.GetComponent<CharacterController>();
        _animAbility = _owner.GetAbility<HelperAnimationAbility>();
    }

    private void Update()
    {
        if (_owner.State != EHelperState.Summoned) return;
        if (_owner.FollowTarget == null) return;

        Vector3 diff = _owner.FollowTarget.position - _owner.transform.position;

        if (TryTeleport(diff)) return;

        ApplyGravity();

        if (_launchVelocity.sqrMagnitude > 0.01f)
        {
            _owner.transform.rotation = Quaternion.Slerp(
                _owner.transform.rotation, _launchTargetRotation,
                _launchRotationSpeed * Time.deltaTime);

            Vector3 velocity = _launchVelocity;
            velocity.y = _yVelocity;
            _cc.Move(velocity * Time.deltaTime);

            if (_cc.isGrounded)
            {
                _launchVelocity = Vector3.zero;
                _animAbility.Play(EHelperAnim.Idle);
            }

            return;
        }

        Move(diff);
    }

    private bool NeedsTeleport(Vector3 diff)
    {
        return diff.magnitude > _teleportDistance || diff.y > _teleportHeightDiff;
    }

    private bool TryTeleport(Vector3 diff)
    {
        if (NeedsTeleport(diff))
        {
            _teleportTimer += Time.deltaTime;
            if (_teleportTimer >= _teleportDelay)
            {
                _teleportTimer = 0f;
                Teleport(_owner.FollowTarget.position);
                return true;
            }
        }
        else
        {
            _teleportTimer = 0f;
        }
        return false;
    }

    private void ApplyGravity()
    {
        if (_cc.isGrounded)
            _yVelocity = GroundedYVelocity;
        else
            _yVelocity -= Gravity * Time.deltaTime;
    }

    private void Move(Vector3 diff)
    {
        Vector3 horizontalDiff = new Vector3(diff.x, 0f, diff.z);
        float horizontalDist = horizontalDiff.magnitude;

        float targetSpeed = CalculateTargetSpeed(horizontalDist);
        _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedSmoothVelocity, _smoothTime);

        if (_currentSpeed > 0.01f)
        {
            Vector3 horizontalDir = horizontalDiff.normalized;

            if (_cc.isGrounded)
                TryAutoJump(horizontalDir);

            Vector3 velocity = horizontalDir * _currentSpeed;
            velocity.y = _yVelocity;

            _cc.Move(velocity * Time.deltaTime);
            RotateToward(horizontalDir);

            if (_cc.isGrounded)
            {
                float speedRatio = _currentSpeed / _moveSpeed;
                _animAbility.Play(speedRatio >= _runSpeedThreshold ? EHelperAnim.Run : EHelperAnim.Walk);
            }
        }
        else
        {
            _currentSpeed = 0f;
            _cc.Move(new Vector3(0f, _yVelocity, 0f) * Time.deltaTime);

            if (_cc.isGrounded)
                _animAbility.Play(EHelperAnim.Idle);
        }
    }

    private float CalculateTargetSpeed(float horizontalDist)
    {
        if (horizontalDist <= _stopDistance) return 0f;

        float t = Mathf.InverseLerp(_stopDistance, _slowDownDistance, horizontalDist);
        return Mathf.Lerp(0f, _moveSpeed, t);
    }

    private void RotateToward(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _owner.transform.rotation = Quaternion.Slerp(
                _owner.transform.rotation, targetRotation,
                _launchRotationSpeed * Time.deltaTime);
        }
    }

    private void TryAutoJump(Vector3 moveDir)
    {
        Vector3 origin = _owner.transform.position + Vector3.up * 0.2f;

        // 앞에 벽이 있는지
        if (!Physics.Raycast(origin, moveDir, _autoJumpCheckDist, _jumpCheckMask)) return;

        // 벽 위가 비어있는지
        Vector3 highOrigin = origin + Vector3.up * _autoJumpMaxHeight;
        if (Physics.Raycast(highOrigin, moveDir, _autoJumpCheckDist, _jumpCheckMask)) return;

        _yVelocity = _autoJumpForce;
        _animAbility.Play(EHelperAnim.Jump);
    }

    public void LaunchBack(Vector3 backDirection)
    {
        _yVelocity = _unequipJumpForce;
        _launchVelocity = backDirection.normalized * _unequipBackForce;
        _launchTargetRotation = Quaternion.LookRotation(backDirection);
        _animAbility.Play(EHelperAnim.Jump);
    }

    private void Teleport(Vector3 targetPos)
    {
        _cc.enabled = false;
        _owner.transform.position = targetPos + _owner.FollowTarget.right * 1.5f;
        _cc.enabled = true;
        _yVelocity = GroundedYVelocity;
    }
}