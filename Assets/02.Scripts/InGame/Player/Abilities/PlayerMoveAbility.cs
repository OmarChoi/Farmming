using UnityEngine;

public class PlayerMoveAbility : PlayerAbility
{
    [SerializeField] private KeyCode _sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode _jumpKey = KeyCode.Space;
    private const float Gravity = 15f;
    private const float GroundedYVelocity = -2f;
    private const float AnimSmoothSpeed = 5f;
    private const float MoveThresholdSqr = 0.01f;
    private const float RunAimValue = 1f;
    private const float WalkAnimValue = 0.5f;
    private const float IdleAnimValue = 0f;
    private CharacterController _characterController;
    private PlayerAnimationAbility _animation;
    private PlayerHelperInteractionAbility _helperInteraction;
    private Camera _mainCamera;

    private float _yVelocity;
    private float _currentMoveParam;
    private bool _wasGrounded = true;

    private void Start()
    {
        _characterController = _owner.GetComponent<CharacterController>();
        _animation = _owner.GetAbility<PlayerAnimationAbility>();
        _helperInteraction = _owner.GetAbility<PlayerHelperInteractionAbility>();
        _mainCamera = Camera.main;

    }

    private void Update()
    {
        if (!_owner.IsMine) return;

        if (!_owner.CanMove)
        {
            UpdateAnimation(false, false);
            UpdateGravity();
            _characterController.Move(new Vector3(0f, _yVelocity, 0f) * Time.deltaTime);
            return;
        }

        Vector3 direction = GetMoveDirection();
        bool isMoving = direction.sqrMagnitude > MoveThresholdSqr;
        bool isHelperEquipped = _helperInteraction.CurrentHelper != null
            && _helperInteraction.CurrentHelper.State == EHelperState.Equipped;
        bool isSprinting = isMoving && Input.GetKey(_sprintKey) && !isHelperEquipped;

        UpdateAnimation(isMoving, isSprinting);
        FaceMovementDirection(direction, isMoving);
        UpdateGravity();
        Move(direction, isSprinting);
    }

    private Vector3 GetMoveDirection()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 direction = _mainCamera.transform.TransformDirection(new Vector3(h, 0f, v));
        direction.y = 0f;

        return direction.normalized;
    }

    private void Move(Vector3 direction, bool isSprinting)
    {
        float speed = isSprinting ? _owner.StatSo.RunSpeed : _owner.StatSo.WalkSpeed;

        Vector3 velocity = direction * speed;
        velocity.y = _yVelocity;

        _characterController.Move(velocity * Time.deltaTime);
    }

    private void FaceMovementDirection(Vector3 direction, bool isMoving)
    {
        if (!isMoving) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        _owner.transform.rotation = Quaternion.Slerp(_owner.transform.rotation, targetRotation, _owner.StatSo.RotationSpeed * Time.deltaTime);
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
            if (Input.GetKeyDown(_jumpKey))
            {
                _yVelocity = _owner.StatSo.JumpPower;
                _animation.SetGrounded(false);
            }
            else if (_yVelocity < 0)
            {
                _yVelocity = GroundedYVelocity;
            }
        }
        else
        {
            if (_wasGrounded)
                _animation.SetGrounded(false);

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