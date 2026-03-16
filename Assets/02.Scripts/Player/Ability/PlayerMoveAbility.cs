using UnityEngine;

public class PlayerMoveAbility : PlayerAbility
{
    private const float Gravity = 9.8f;
    private const float AnimSmoothSpeed = 5f;
    private const float MoveThresholdSqr = 0.01f;
    private const float GroundedYVelocity = -0.5f;
    private const float RunAimValue = 1f;
    private const float WalkAnimValue = 0.5f;
    private const float IdleAnimValue = 0f;
    private CharacterController _characterController;
    private PlayerAnimationAbility _animation;
    private Camera _mainCamera;

    private float _yVelocity;
    private float _currentMoveParam;
    private bool _wasGrounded = true;

    private void Start()
    {
        _characterController = _owner.GetComponent<CharacterController>();
        _animation = _owner.GetAbility<PlayerAnimationAbility>();
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        Vector3 direction = GetMoveDirection();
        bool isMoving = direction.sqrMagnitude > MoveThresholdSqr;
        bool isSprinting = isMoving && Input.GetKey(KeyCode.LeftShift);

        UpdateAnimation(isMoving, isSprinting);
        FaceMovementDirection(direction, isMoving);
        UpdateGravity();
        Move(direction, isSprinting);
    }

    private Vector3 GetMoveDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 direction = _mainCamera.transform.TransformDirection(new Vector3(h, 0f, v));
        direction.y = 0f;

        return direction.normalized;
    }

    private void Move(Vector3 direction, bool isSprinting)
    {
        float speed = isSprinting ? _owner.Stat.RunSpeed : _owner.Stat.WalkSpeed;

        Vector3 velocity = direction * speed;
        velocity.y = _yVelocity;

        _characterController.Move(velocity * Time.deltaTime);
    }

    private void FaceMovementDirection(Vector3 direction, bool isMoving)
    {
        if (!isMoving) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        _owner.transform.rotation = Quaternion.Slerp(_owner.transform.rotation, targetRotation, _owner.Stat.RotationSpeed * Time.deltaTime);
    }

    private void UpdateGravity()
    {
        bool isGrounded = _characterController.isGrounded;

        if (isGrounded && !_wasGrounded)
        {
            _animation.SetGrounded(true);
        }

        if (isGrounded)
        {
            _yVelocity = GroundedYVelocity;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _yVelocity = _owner.Stat.JumpPower;
                _animation.TriggerJump();
                _animation.SetGrounded(false);
            }
        }
        else
        {
            _yVelocity -= Gravity * Time.deltaTime;
        }

        _wasGrounded = isGrounded;
    }

    private void UpdateAnimation(bool isMoving, bool isSprinting)
    {
        float targetParam = isSprinting ? RunAimValue : isMoving ? WalkAnimValue : IdleAnimValue;
        _currentMoveParam = Mathf.Lerp(_currentMoveParam, targetParam, AnimSmoothSpeed * Time.deltaTime);
        _animation.SetMove(_currentMoveParam);
    }
}