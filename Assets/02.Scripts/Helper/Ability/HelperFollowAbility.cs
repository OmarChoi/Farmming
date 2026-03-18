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

    private const float Gravity = 9.8f;
    private const float GroundedYVelocity = -0.5f;

    private CharacterController _cc;
    private HelperAnimationAbility _animAbility;
    private float _yVelocity;
    private float _currentSpeed;
    private float _speedSmoothVelocity;
    private float _teleportTimer;

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
            Vector3 velocity = horizontalDir * _currentSpeed;
            velocity.y = _yVelocity;

            _cc.Move(velocity * Time.deltaTime);
            RotateToward(horizontalDir);
            float speedRatio = _currentSpeed / _moveSpeed;
            _animAbility.Play(speedRatio >= _runSpeedThreshold ? EHelperAnim.Run : EHelperAnim.Walk);
        }
        else
        {
            _currentSpeed = 0f;
            _cc.Move(new Vector3(0f, _yVelocity, 0f) * Time.deltaTime);
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
            _owner.transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Teleport(Vector3 targetPos)
    {
        _cc.enabled = false;
        _owner.transform.position = targetPos + _owner.FollowTarget.right * 1.5f;
        _cc.enabled = true;
        _yVelocity = GroundedYVelocity;
    }
}