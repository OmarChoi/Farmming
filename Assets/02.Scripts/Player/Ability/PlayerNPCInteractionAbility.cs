using UnityEngine;

public class PlayerNPCInteractionAbility : PlayerAbility
{
    [SerializeField] private float _radius = 5f;
    [SerializeField] private LayerMask _interactionLayer;
    [SerializeField] private KeyCode _interactKey = KeyCode.P;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private CameraPreset _cameraPreset;

    private PlayerCameraAbility _cameraAbility;
    private PlayerAnimationAbility _animation;
    private Transform _faceTarget;

    protected override void Awake()
    {
        base.Awake();
        _cameraAbility = _owner.GetAbility<PlayerCameraAbility>();
        _animation = _owner.GetAbility<PlayerAnimationAbility>();
    }

    private void Update()
    {
        if (_faceTarget != null)
        {
            RotateTowardTarget();
            return;
        }

        if (!_owner.CanMove) return;

        if (Input.GetKeyDown(_interactKey))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _radius, _interactionLayer);

        INpcInteraction closest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            INpcInteraction interactable = hit.GetComponentInParent<INpcInteraction>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                closest = interactable;
            }
        }

        if (closest != null)
        {
            _faceTarget = (closest as Component).transform;
            _owner.LockAction();
            _animation?.PlayGreet();
            _cameraAbility?.SetPreset(_cameraPreset, _faceTarget);
            closest.RequestInteract(transform);
        }
    }

    public void EndInteraction()
    {
        _faceTarget = null;
        _cameraAbility?.ClearPreset();
        _owner.UnlockAction();
    }

    private void RotateTowardTarget()
    {
        Vector3 dir = _faceTarget.position - _owner.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        _owner.transform.rotation = Quaternion.Slerp(
            _owner.transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }
}
